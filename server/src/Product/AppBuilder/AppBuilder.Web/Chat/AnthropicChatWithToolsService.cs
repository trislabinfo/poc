using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppBuilder.Web.Chat;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppBuilder.Web;

public sealed class AnthropicChatWithToolsService
{
    private readonly AppBuilderWebOptions _options;
    private readonly IAppBuilderToolExecutor _toolExecutor;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnthropicChatWithToolsService> _logger;

    public AnthropicChatWithToolsService(
        IOptions<AppBuilderWebOptions> options,
        IAppBuilderToolExecutor toolExecutor,
        IHttpClientFactory httpClientFactory,
        ILogger<AnthropicChatWithToolsService> logger)
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
            model = "claude-3-5-sonnet-20240620";

        // Anthropic's Messages API expects role: user/assistant and content blocks.
        var messages = history
            .Select(m => new Dictionary<string, object?>
            {
                ["role"] = m.Role,
                ["content"] = new object[]
                {
                    new Dictionary<string, object?>
                    {
                        ["type"] = "text",
                        ["text"] = m.Content
                    }
                }
            })
            .ToList();

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/", UriKind.Absolute);
        httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var system = "You help users create and manage application definitions in the AppBuilder service. Use MCP tools when you need to create, read, or update application definitions.";
        var tools = BuildToolDefinitions();

        for (var iteration = 0; iteration < 5; iteration++)
        {
            var payload = new
            {
                model,
                system,
                max_tokens = 1024,
                messages,
                tools,
                tool_choice = new { type = "auto" }
            };

            using var response = await httpClient.PostAsJsonAsync("messages", payload, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Anthropic request failed ({(int)response.StatusCode}): {responseJson}");

            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var content = root.GetProperty("content");
            var toolUses = new List<(string id, string name, Dictionary<string, object?> input)>();
            var textParts = new List<string>();

            foreach (var block in content.EnumerateArray())
            {
                var type = block.GetProperty("type").GetString();
                if (string.Equals(type, "text", StringComparison.OrdinalIgnoreCase))
                {
                    if (block.TryGetProperty("text", out var textEl))
                        textParts.Add(textEl.GetString() ?? string.Empty);
                }
                else if (string.Equals(type, "tool_use", StringComparison.OrdinalIgnoreCase))
                {
                    var id = block.GetProperty("id").GetString() ?? string.Empty;
                    var name = block.GetProperty("name").GetString() ?? string.Empty;
                    var inputEl = block.GetProperty("input");
                    var input = ParseArgumentsObject(inputEl);
                    toolUses.Add((id, name, input));
                }
            }

            if (toolUses.Count == 0)
            {
                var final = string.Join("", textParts).Trim();
                return final;
            }

            // Append assistant message with tool_use blocks
            messages.Add(new Dictionary<string, object?>
            {
                ["role"] = "assistant",
                ["content"] = toolUses.Select(t => (object)new Dictionary<string, object?>
                {
                    ["type"] = "tool_use",
                    ["id"] = t.id,
                    ["name"] = t.name,
                    ["input"] = t.input
                }).ToArray()
            });

            // Execute tools and append user message with tool_result blocks
            var toolResultBlocks = new List<object>();
            foreach (var (id, name, input) in toolUses)
            {
                var toolResultText = await _toolExecutor.ExecuteToolAsync(name, input, cancellationToken);
                toolResultBlocks.Add(new Dictionary<string, object?>
                {
                    ["type"] = "tool_result",
                    ["tool_use_id"] = id,
                    ["content"] = toolResultText
                });
            }

            messages.Add(new Dictionary<string, object?>
            {
                ["role"] = "user",
                ["content"] = toolResultBlocks
            });
        }

        _logger.LogWarning("Anthropic tool-call loop hit max iterations without final content.");
        return string.Join("", history.Where(m => m.Role == "assistant").Select(m => m.Content)).Trim();
    }

    private static object[] BuildToolDefinitions()
    {
        return new object[]
        {
            new
            {
                name = "application_definitions.create",
                description = "Create a new application definition in AppBuilder.",
                input_schema = new
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
            },
            new
            {
                name = "application_definitions.get",
                description = "Get an application definition by id.",
                input_schema = new
                {
                    type = "object",
                    properties = new
                    {
                        id = new { type = "string", description = "Application definition id (UUID)" }
                    },
                    required = new[] { "id" }
                }
            },
            new
            {
                name = "application_definitions.update",
                description = "Update an existing application definition's name and description.",
                input_schema = new
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
        };
    }

    private static Dictionary<string, object?> ParseArgumentsObject(JsonElement inputEl)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (inputEl.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in inputEl.EnumerateObject())
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

