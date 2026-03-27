namespace PlatformMetaModel.Lifecycle.Common;

/// <summary>
/// DDD-related common properties for aggregate roots/entities: optimistic concurrency version.
/// </summary>
public class DomainModelDefinition
{
    /// <summary>
    /// Optimistic concurrency version (DDD aggregate version). Incremented on each update.
    /// </summary>
    public required int Version { get; set; }
}
