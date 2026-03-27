using BuildingBlocks.Application.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Capabilities.Logging.Serilog;

public static class SerilogExtensions
{
    public static WebApplicationBuilder AddSerilogStructuredLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        });

        builder.Services.AddSingleton<IStructuredLogger>(_ => new SerilogStructuredLogger(Log.Logger));

        return builder;
    }
}
