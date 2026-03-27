using System.Text.Json.Serialization;

namespace PlatformMetaModel.Permission;

/// <summary>
/// Permission definition; defined only inside application definition.
/// </summary>
public class PermissionDefinition
{
    public required ResourceType ResourceType { get; set; }

    public required string ResourceId { get; set; }

    public required IList<PermissionAction> Actions { get; set; }

    public bool TenantScoped { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResourceType
{
    Entity,
    Page,
    Workflow
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PermissionAction
{
    Create,
    Read,
    Update,
    Delete,
    Execute
}
