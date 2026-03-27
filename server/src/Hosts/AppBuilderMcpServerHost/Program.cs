using AppBuilder.McpServer;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

var appBuilderApiBaseUrl =
    builder.Configuration["AppBuilderApi:BaseUrl"]
    ?? throw new InvalidOperationException("Missing configuration value: AppBuilderApi:BaseUrl");

builder.Services.AddHttpClient<AppBuilderApiClient>(client =>
{
    client.BaseAddress = new Uri(appBuilderApiBaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<McpAuthForwardingHandler>();

builder.Services.AddHttpContextAccessor();

var mcpToolsAssembly = typeof(AppBuilderApplicationDefinitionTools).Assembly;
var mcpPromptsAssembly = typeof(AppBuilderApplicationDefinitionPrompts).Assembly;

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly(mcpToolsAssembly)
    .WithPromptsFromAssembly(mcpPromptsAssembly);

var app = builder.Build();

// MCP clients connect via HTTP; expose at a stable route.
app.MapMcp("/mcp");

app.MapGet("/", () => "AppBuilder MCP server is running.");

app.Run();
