using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppBuilder.Web;

public sealed class McpAppBuilderToolExecutor : IAppBuilderToolExecutor
{
    private readonly AppBuilderWebOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILoggerFactory _loggerFactory;

    public McpAppBuilderToolExecutor(
        IOptions<AppBuilderWebOptions> options,
        IHttpClientFactory httpClientFactory,
        ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _loggerFactory = loggerFactory;
    }

    public async Task<string> ExecuteToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            throw new ArgumentException("toolName is required.", nameof(toolName));

        var mcpEndpointUrl = _options.Mcp.BaseUrl;
        if (string.IsNullOrWhiteSpace(mcpEndpointUrl))
            throw new InvalidOperationException("AppBuilderWeb:Mcp:BaseUrl is missing.");

        // HttpClientTransportOptions.Endpoint expects the full MCP endpoint URL (including `/mcp`).
        // For convenience, accept both:
        // - http://host:port/mcp (preferred)
        // - http://host:port (will be normalized)
        var endpointUrl = mcpEndpointUrl.Trim().TrimEnd('/');
        if (!endpointUrl.EndsWith("/mcp", StringComparison.OrdinalIgnoreCase))
            endpointUrl += "/mcp";

        var endpoint = new Uri(endpointUrl, UriKind.Absolute);

        using var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        var transportOptions = new HttpClientTransportOptions
        {
            Endpoint = endpoint
        };

        await using var transport = new HttpClientTransport(transportOptions, httpClient, _loggerFactory, ownsHttpClient: false);
        await using var mcpClient = await McpClient.CreateAsync(transport, null, _loggerFactory, cancellationToken);

        var toolResult = await mcpClient.CallToolAsync(
            toolName,
            arguments,
            cancellationToken: cancellationToken);

        if (toolResult.StructuredContent is not null)
            return toolResult.StructuredContent.Value.GetRawText();

        // Extract readable text from tool result content blocks.
        var parts = new List<string>(capacity: 1);
        foreach (var block in toolResult.Content)
        {
            if (block is TextContentBlock textBlock)
                parts.Add(textBlock.Text);
            else
                parts.Add(block.ToString() ?? string.Empty);
        }

        var combined = string.Join("\n", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        if (!string.IsNullOrWhiteSpace(combined))
            return combined;

        // Fallback.
        return toolResult.ToString() ?? string.Empty;
    }
}

