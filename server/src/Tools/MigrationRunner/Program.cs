using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MigrationRunner.Services;

var dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
var rollback = args.Contains("--rollback", StringComparer.OrdinalIgnoreCase);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        var basePath = AppContext.BaseDirectory;
        config.AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: true, reloadOnChange: false);
        var topologyIndex = Array.FindIndex(args, a => a.Equals("--topology", StringComparison.OrdinalIgnoreCase));
        if (topologyIndex >= 0 && topologyIndex + 1 < args.Length)
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Deployment:Topology"] = args[topologyIndex + 1]
            });
        var moduleIndex = Array.FindIndex(args, a => a.Equals("--module", StringComparison.OrdinalIgnoreCase));
        if (moduleIndex >= 0 && moduleIndex + 1 < args.Length)
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["MigrationRunner:Module"] = args[moduleIndex + 1]
            });
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IModuleDiscovery, ModuleDiscovery>();
        services.AddSingleton<IDependencyGraphResolver, DependencyGraphResolver>();
        services.AddSingleton<IMigrationOrchestrator, MigrationOrchestrator>();
    })
    .Build();
var orchestrator = host.Services.GetRequiredService<IMigrationOrchestrator>();

try
{
    if (rollback)
        await orchestrator.RollbackAsync(dryRun);
    else
        await orchestrator.MigrateAsync(dryRun);
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Migration failed.");
    Environment.ExitCode = 1;
}
