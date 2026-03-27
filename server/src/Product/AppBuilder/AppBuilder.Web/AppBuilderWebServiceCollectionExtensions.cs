using Blazorise;
using Blazorise.Bootstrap5;
using Blazorise.Icons.FontAwesome;
using BuildingBlocks.Web.AdminNavigation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AppBuilder.Web;

public static class AppBuilderWebServiceCollectionExtensions
{
    public static IServiceCollection AddAppBuilderWeb(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AppBuilderWebOptions>(configuration.GetSection("AppBuilderWeb"));

        // Register Blazorise UI services.
        // Note: CSS is wired from the hosting App.razor (see AppBuilderClientHost).
        services
            .AddBlazorise(options => options.Immediate = true)
            .AddBootstrap5Providers()
            .AddFontAwesomeIcons();

        // Chat (UI -> orchestrator -> LLM -> tool execution via MCP).
        services.AddScoped<IAppBuilderChatService, AppBuilderChatService>();
        services.AddScoped<IAppBuilderToolExecutor, McpAppBuilderToolExecutor>();

        services.AddScoped<OpenAiChatWithToolsService>();
        services.AddScoped<AnthropicChatWithToolsService>();

        // Admin navigation for the host layout.
        services.AddScoped<IAdminNavigationProvider, AppBuilderChatAdminNavigationProvider>();

        // Helpful default for UI components.
        services.AddHttpClient();

        return services;
    }
}

