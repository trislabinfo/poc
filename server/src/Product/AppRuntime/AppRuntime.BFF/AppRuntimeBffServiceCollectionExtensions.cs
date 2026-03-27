using AppRuntime.BFF.Services;
using AppRuntime.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AppRuntime.BFF;

public static class AppRuntimeBffServiceCollectionExtensions
{
    /// <summary>Adds Runtime BFF controllers and in-process backend (TenantApplication, AppBuilder, AppRuntime in same process).</summary>
    public static IServiceCollection AddRuntimeBff(this IServiceCollection services)
    {
        services.AddScoped<IReleaseSnapshotProvider, BffReleaseSnapshotProvider>();
        services.AddScoped<IRuntimeApi, InProcessRuntimeApi>();
        services.AddControllers()
            .AddApplicationPart(typeof(Controllers.RuntimeBffController).Assembly);
        return services;
    }

    /// <summary>Adds Runtime BFF controllers only; register <see cref="IRuntimeApi"/> yourself (e.g. <see cref="MonolithHttpRuntimeApi"/> when calling Monolith). Does not register <see cref="IReleaseSnapshotProvider"/> (not needed when backend is remote).</summary>
    public static IServiceCollection AddRuntimeBffControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(Controllers.RuntimeBffController).Assembly);
        return services;
    }
}
