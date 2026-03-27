using BuildingBlocks.Web.AdminNavigation;
using BuildingBlocks.Web.Rest;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using User.Web.Clients;

namespace User.Web;

public static class UserWebServiceCollectionExtensions
{
    public static IServiceCollection AddUserWeb(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAdminNavigationProvider, UserAdminNavigationProvider>();
        services.AddFrontendApiClient<IUserApiClient, UserApiClient>("user");
        return services;
    }
}

