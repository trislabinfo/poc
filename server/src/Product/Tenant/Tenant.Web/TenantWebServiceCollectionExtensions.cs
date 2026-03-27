using BuildingBlocks.Web.AdminNavigation;
using BuildingBlocks.Web.Rest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tenant.Web.Clients;

namespace Tenant.Web;

public static class TenantWebServiceCollectionExtensions
{
    public static IServiceCollection AddTenantWeb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAdminNavigationProvider, TenantAdminNavigationProvider>();

        services.AddFrontendApiClient<ITenantApiClient, TenantApiClient>("tenant");

        return services;
    }
}

