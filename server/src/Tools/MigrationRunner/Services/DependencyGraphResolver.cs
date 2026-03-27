namespace MigrationRunner.Services;

/// <summary>
/// Topological sort of modules by their migration dependencies.
/// </summary>
public class DependencyGraphResolver : IDependencyGraphResolver
{
    /// <inheritdoc />
    public string[] ResolveOrder(string[] moduleNames, Func<string, string[]> getDependencies)
    {
        var set = moduleNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var order = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Visit(string name)
        {
            if (visited.Contains(name)) return;
            if (visiting.Contains(name))
                throw new InvalidOperationException($"Circular dependency involving module '{name}'.");
            visiting.Add(name);
            var deps = getDependencies(name) ?? Array.Empty<string>();
            foreach (var dep in deps)
                if (set.Contains(dep))
                    Visit(dep);
            visiting.Remove(name);
            visited.Add(name);
            order.Add(name);
        }

        foreach (var name in moduleNames)
            Visit(name);

        return order.ToArray();
    }
}
