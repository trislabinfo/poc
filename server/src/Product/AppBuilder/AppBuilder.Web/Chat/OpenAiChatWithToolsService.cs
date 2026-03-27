using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppBuilder.Web.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppBuilder.Web;

public sealed class OpenAiChatWithToolsService
{
    private readonly AppBuilderWebOptions _options;
    private readonly IAppBuilderToolExecutor _toolExecutor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<OpenAiChatWithToolsService> _logger;

    public OpenAiChatWithToolsService(
        IOptions<AppBuilderWebOptions> options,
        IAppBuilderToolExecutor toolExecutor,
        IHttpClientFactory httpClientFactory,
        ILogger<OpenAiChatWithToolsService> logger)
    {
        _options = options.Value;
        _toolExecutor = toolExecutor;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> ChatAsync(
        IReadOnlyList<ChatMessage> history,
        CancellationToken cancellationToken = default)
    {
        var apiKey = _options.Llm.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("AppBuilderWeb:Llm:ApiKey is missing.");

        var model = _options.Llm.Model;
        if (string.IsNullOrWhiteSpace(model))
            model = "gpt-4o-mini";

        // Keep message structure aligned with OpenAI tool-calling format.
        var messages = new List<Dictionary<string, object?>>();
        messages.Add(new Dictionary<string, object?>
        {
            ["role"] = "system",
            ["content"] = "You help users create and manage application definitions in the AppBuilder service. Use MCP tools when you need to create, read, or update application definitions."
        });

        foreach (var msg in history)
        {
            messages.Add(new Dictionary<string, object?>
            {
                ["role"] = msg.Role,
                ["content"] = msg.Content
            });
        }

        var tools = BuildToolDefinitions();

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.openai.com/v1/", UriKind.Absolute);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        for (var iteration = 0; iteration < 5; iteration++)
        {
            var payload = new
            {
                model,
                messages,
                tools,
                tool_choice = "auto"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            request.Content = JsonContent.Create(payload);

            using var response = await httpClient.SendAsync(request, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"OpenAI request failed ({(int)response.StatusCode}): {responseJson}");

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var choice = root.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            var content = message.TryGetProperty("content", out var contentEl) ? contentEl.GetString() : null;

            if (message.TryGetProperty("tool_calls", out var toolCallsEl) && toolCallsEl.ValueKind == JsonValueKind.Array)
            {
                var toolCalls = new List<(string id, string name, string argumentsJson)>();
                foreach (var toolCallEl in toolCallsEl.EnumerateArray())
                {
                    var id = toolCallEl.GetProperty("id").GetString() ?? string.Empty;
                    var fn = toolCallEl.GetProperty("function");
                    var name = fn.GetProperty("name").GetString() ?? string.Empty;
                    var argsJson = fn.GetProperty("arguments").GetString() ?? "{}";

                    toolCalls.Add((id, name, argsJson));
                }

                // Append the assistant tool call message.
                messages.Add(new Dictionary<string, object?>
                {
                    ["role"] = "assistant",
                    ["content"] = content,
                    ["tool_calls"] = toolCalls.Select(tc => new Dictionary<string, object?>
                    {
                        ["id"] = tc.id,
                        ["type"] = "function",
                        ["function"] = new Dictionary<string, object?>
                        {
                            ["name"] = tc.name,
                            ["arguments"] = tc.argumentsJson
                        }
                    }).ToList()
                });

                // Execute tool calls via MCP.
                foreach (var (id, name, argsJson) in toolCalls)
                {
                    var args = ParseArguments(argsJson);
                    var toolResultText = await _toolExecutor.ExecuteToolAsync(name, args, cancellationToken);

                    // Append tool result message.
                    messages.Add(new Dictionary<string, object?>
                    {
                        ["role"] = "tool",
                        ["tool_call_id"] = id,
                        ["content"] = toolResultText
                    });
                }

                continue;
            }

            if (!string.IsNullOrWhiteSpace(content))
                return content;

            // If we didn't get tool calls and no content, keep going once.
            return content ?? string.Empty;
        }

        _logger.LogWarning("OpenAI tool-call loop hit max iterations without final content.");
        return string.Empty;
    }

    private static object[] BuildToolDefinitions()
    {
        // JSON schema for OpenAI "function" tools.
        // Note: tool parameter names must match the MCP tool method parameters (Create/Get/Update).
        return new object[]
        {
            new
            {
                type = "function",
                function = new
                {
                    name = "application_definitions.create",
                    description = "Create a new application definition in AppBuilder.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string", description = "Application definition name" },
                            description = new { type = "string", description = "Application definition description" },
                            slug = new { type = "string", description = "Unique slug (server normalizes)" },
                            isPublic = new { type = "boolean", description = "Whether the application is public" }
                        },
                        required = new[] { "name", "description", "slug", "isPublic" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "application_definitions.get",
                    description = "Get an application definition by id.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "Application definition id (UUID)" }
                        },
                        required = new[] { "id" }
                    }
                }
            },
            new
            {
                type = "function",
                function = new
                {
                    name = "application_definitions.update",
                    description = "Update an existing application definition's name and description.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            id = new { type = "string", description = "Application definition id (UUID)" },
                            name = new { type = "string", description = "Updated application name" },
                            description = new { type = "string", description = "Updated application description" }
                        },
                        required = new[] { "id", "name", "description" }
                    }
                }
            }
        };
    }

    private static Dictionary<string, object?> ParseArguments(string argsJson)
    {
        if (string.IsNullOrWhiteSpace(argsJson))
            return new Dictionary<string, object?>();

        using var doc = JsonDocument.Parse(argsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
            return new Dictionary<string, object?>();

        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in doc.RootElement.EnumerateObject())
            result[prop.Name] = ConvertJsonValue(prop.Value);

        return result;
    }

    private static object? ConvertJsonValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.TryGetInt64(out var l) ? l : value.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.GetRawText()
        };
    }
}

