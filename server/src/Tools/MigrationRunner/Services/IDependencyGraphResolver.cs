namespace MigrationRunner.Services;

/// <summary>
/// Resolves migration order by module dependencies (topological sort).
/// </summary>
public interface IDependencyGraphResolver
{
    /// <summary>
    /// Returns module names in an order safe for running migrations (dependencies first).
    /// </summary>
    /// <param name="moduleNames">Module names to order.</param>
    /// <param name="getDependencies">Returns dependency module names for a given module.</param>
    string[] ResolveOrder(string[] moduleNames, Func<string, string[]> getDependencies);
}
