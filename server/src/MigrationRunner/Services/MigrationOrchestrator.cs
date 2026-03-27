using FluentMigrator.Runner;
using FluentMigrator.Runner.VersionTableInfo;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace MigrationRunner.Services;

/// <summary>
/// Runs FluentMigrator migrations per module.
/// </summary>
public class MigrationOrchestrator : IMigrationOrchestrator
{
    private readonly IModuleDiscovery _discovery;
    private readonly IDependencyGraphResolver _resolver;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MigrationOrchestrator> _logger;

    private static Assembly? GetMigrationAssembly(string moduleName, ILogger logger)
    {
        var assemblyName = $"{moduleName}.Migrations";
        try
        {
            return Assembly.Load(assemblyName);
        }
        catch
        {
            logger.LogError($"cannot load assembly {assemblyName}.");
            return null;
        }
    }

    /// <summary>
    /// Gets a display name for the migration (type name if available, otherwise version).
    /// </summary>
    private static string GetMigrationName(object? migrationInfo, long version)
    {
        if (migrationInfo is null) return version.ToString();
        var type = migrationInfo.GetType().GetProperty("MigrationType")?.GetValue(migrationInfo) as Type;
        return type?.Name ?? version.ToString();
    }

    public MigrationOrchestrator(
        IModuleDiscovery discovery,
        IDependencyGraphResolver resolver,
        IConfiguration configuration,
        ILogger<MigrationOrchestrator> logger)
    {
        _discovery = discovery;
        _resolver = resolver;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gets the connection string, applying PGPORT override when set (e.g. when Postgres runs in Docker with a different host port).
    /// </summary>
    private string GetConnectionString()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
        var portOverride = Environment.GetEnvironmentVariable("PGPORT");
        if (!string.IsNullOrWhiteSpace(portOverride))
        {
            // Replace Port= in the connection string so Docker host-mapped port can be used
            connectionString = System.Text.RegularExpressions.Regex.Replace(
                connectionString,
                @"Port=\d+",
                $"Port={portOverride.Trim()}",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        return connectionString;
    }

    /// <inheritdoc />
    public async Task MigrateAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        var modules = _discovery.GetModulesForCurrentTopology();
        var ordered = _resolver.ResolveOrder(modules, _ => Array.Empty<string>());

        _logger.LogInformation("Migration order: {Order}", string.Join(" -> ", ordered));

        if (dryRun)
        {
            _logger.LogInformation("[Dry run] Would run migrations for modules: {Modules}", string.Join(", ", ordered));
            await Task.CompletedTask;
            return;
        }

        var connectionString = GetConnectionString();

        foreach (var moduleName in ordered)
        {
            var assembly = GetMigrationAssembly(moduleName, _logger);
            if (assembly is null)
            {
                _logger.LogWarning("Unknown or unloadable module '{Module}', skipping.", moduleName);
                continue;
            }

            _logger.LogInformation("Running migrations for module '{Module}'...", moduleName);
            var versionMeta = new ModuleVersionTableMetaData(moduleName);
            VersionTableEnsurer.EnsureVersionTableAndIndex(connectionString, versionMeta, _logger);

            var serviceCollection = new ServiceCollection()
                .AddSingleton<IConfiguration>(_configuration)
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(assembly).For.Migrations());
            serviceCollection.AddScoped<IVersionTableMetaData>(_ => versionMeta);
            serviceCollection.AddScoped<IVersionLoader, IdempotentVersionLoader>();
            var services = serviceCollection.BuildServiceProvider(false);

            await using (services as IAsyncDisposable)
            {
                using var scope = services.CreateScope();
                var versionLoader = scope.ServiceProvider.GetRequiredService<IVersionLoader>();
                versionLoader.LoadVersionInfo();

                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                var allMigrations = runner.MigrationLoader.LoadMigrations().ToList();
                var pendingCount = allMigrations.Count(m => !versionLoader.VersionInfo.HasAppliedMigration(m.Key));
                var appliedCount = allMigrations.Count - pendingCount;

                if (pendingCount == 0)
                {
                    _logger.LogInformation(
                        "Module '{Module}': {Total} migrations in assembly, {Applied} already applied. No pending migrations.",
                        moduleName, allMigrations.Count, appliedCount);
                }
                else
                {
                    _logger.LogInformation(
                        "Module '{Module}': {Pending} pending of {Total} migrations (already applied: {Applied}).",
                        moduleName, pendingCount, allMigrations.Count, appliedCount);
                    var pendingOrdered = allMigrations
                        .Where(m => !versionLoader.VersionInfo.HasAppliedMigration(m.Key))
                        .OrderBy(m => m.Key)
                        .ToList();
                    foreach (var pair in pendingOrdered)
                    {
                        var name = GetMigrationName(pair.Value, pair.Key);
                        _logger.LogInformation(
                            "Module '{Module}': Applying migration {MigrationName} (version {Version}).",
                            moduleName, name, pair.Key);
                        runner.MigrateUp(pair.Key);
                    }
                }
            }

            _logger.LogInformation("Completed migrations for module '{Module}'.", moduleName);
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        var modules = _discovery.GetModulesForCurrentTopology();
        var ordered = _resolver.ResolveOrder(modules, _ => Array.Empty<string>()).Reverse().ToArray();

        if (dryRun)
        {
            _logger.LogInformation("[Dry run] Would rollback modules in order: {Order}", string.Join(", ", ordered));
            await Task.CompletedTask;
            return;
        }

        var connectionString = GetConnectionString();

        foreach (var moduleName in ordered)
        {
            var assembly = GetMigrationAssembly(moduleName, _logger);
            if (assembly is null)
                continue;

            _logger.LogInformation("Rolling back one step for module '{Module}'...", moduleName);
            var versionMeta = new ModuleVersionTableMetaData(moduleName);
            VersionTableEnsurer.EnsureVersionTableAndIndex(connectionString, versionMeta, _logger);

            var serviceCollection = new ServiceCollection()
                .AddSingleton<IConfiguration>(_configuration)
                .AddFluentMigratorCore()
                .ConfigureRunner(rb => rb
                    .AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(assembly).For.Migrations());
            serviceCollection.AddScoped<IVersionTableMetaData>(_ => versionMeta);
            serviceCollection.AddScoped<IVersionLoader, IdempotentVersionLoader>();
            var services = serviceCollection.BuildServiceProvider(false);

            await using (services as IAsyncDisposable)
            {
                using var scope = services.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
                runner.MigrateDown(1);
            }
        }
    }
}
