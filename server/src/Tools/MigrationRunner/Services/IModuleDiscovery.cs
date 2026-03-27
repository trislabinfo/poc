namespace MigrationRunner.Services;

/// <summary>
/// Discovers which modules to run migrations for based on topology configuration.
/// </summary>
public interface IModuleDiscovery
{
    /// <summary>
    /// Returns the list of module names to run for the current topology.
    /// </summary>
    string[] GetModulesForCurrentTopology();
}
