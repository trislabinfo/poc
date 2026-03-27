using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web.Rest;

/// <summary>
/// Shared HttpClient registration for frontend modules calling backend REST APIs.
/// </summary>
public static class FrontendRestClientRegistrationExtensions
{
    public static IServiceCollection AddFrontendApiClient<TClient, TImplementation>(
        this IServiceCollection services,
        string serviceName)
        where TClient : class
        where TImplementation : class, TClient
    {
        services.AddHttpClient<TClient, TImplementation>((sp, client) =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var baseUrl = ResolveApiBaseUrl(config, serviceName);

            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }

    private static string ResolveApiBaseUrl(IConfiguration configuration, string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            throw new ArgumentException("serviceName is required.", nameof(serviceName));

        var pascalServiceName = char.ToUpperInvariant(serviceName[0]) + serviceName[1..];

        var baseUrl =
            configuration[$"Services:{serviceName}:http"]
            ?? configuration[$"Services:{pascalServiceName}:BaseUrl"]
            ?? configuration["Services:monolith:http"]
            ?? configuration["Services:Monolith:Http"]
            ?? configuration["ControlPlanClient:ApiBaseUrl"]
            ?? "http://localhost:8080/";

        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";

        return baseUrl;
    }
}

