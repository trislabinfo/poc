using Microsoft.Extensions.DependencyInjection;
using TenantApplication.Api.Controllers;

namespace TenantApplication.Api;

public static class TenantApplicationApiServiceCollectionExtensions
{
    /// <summary>Registers TenantApplication API layer (controllers from this assembly).</summary>
    public static IServiceCollection AddTenantApplicationApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(TenantApplicationController).Assembly);
        return services;
    }
}
