using Microsoft.Extensions.DependencyInjection;
using User.Api.Controllers;

namespace User.Api;

public static class UserApiServiceCollectionExtensions
{
    /// <summary>
    /// Registers User API layer (controllers from this assembly).
    /// </summary>
    public static IServiceCollection AddUserApi(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(UserController).Assembly);
        return services;
    }
}
