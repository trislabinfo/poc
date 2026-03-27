namespace PlatformMetaModel.Role;

/// <summary>
/// Role definition; defined only inside application definition.
/// </summary>
public class RoleDefinition
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public IList<string>? Inherits { get; set; }

    public bool TenantScoped { get; set; }
}
