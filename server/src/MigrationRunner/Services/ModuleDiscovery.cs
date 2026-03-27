using Microsoft.Extensions.Configuration;

namespace MigrationRunner.Services;

/// <summary>
/// Reads module list from configuration based on Deployment:Topology.
/// </summary>
public class ModuleDiscovery : IModuleDiscovery
{
    private readonly IConfiguration _configuration;

    public ModuleDiscovery(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public string[] GetModulesForCurrentTopology()
    {
        var singleModule = _configuration["MigrationRunner:Module"];
        if (!string.IsNullOrWhiteSpace(singleModule))
        {
            return [singleModule.Trim()];
        }

        var topology = _configuration["Deployment:Topology"] ?? "Monolith";
        var modules = _configuration
            .GetSection("MigrationRunner:ModulesByTopology")
            .GetSection(topology)
            .Get<string[]>();
        return modules ?? Array.Empty<string>();
    }
}
