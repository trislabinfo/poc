namespace AppBuilder.Web;

public sealed class AppBuilderWebOptions
{
    public LlmOptions Llm { get; set; } = new();
    public McpOptions Mcp { get; set; } = new();

    public sealed class LlmOptions
    {
        /// <summary>Supported values: OpenAI, Anthropic</summary>
        public string Provider { get; set; } = "OpenAI";
        public string Model { get; set; } = "gpt-4o-mini";

        /// <summary>Provider API key (OpenAI Authorization Bearer token or Anthropic x-api-key).</summary>
        public string? ApiKey { get; set; }
    }

    public sealed class McpOptions
    {
        /// <summary>MCP endpoint URL of AppBuilderMcpServerHost (e.g. http://localhost:5185/mcp).</summary>
        public string BaseUrl { get; set; } = "http://localhost:5185";
    }
}

