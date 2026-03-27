using System.Text.Json;

namespace PlatformMetaModel.Lifecycle.TenantApplication;

/// <summary>
/// Tenant-level overrides or customizations applied on top of a base application release.
/// Used when tenant modifies an installed application.
/// </summary>
public class TenantApplicationOverridesDefinition
{
    /// <summary>Override or additional pages (structure conforms to application meta model page definitions).</summary>
    public IList<JsonElement>? Pages { get; set; }

    /// <summary>Override or additional navigation definitions.</summary>
    public IList<JsonElement>? Navigation { get; set; }

    /// <summary>Override or additional permissions.</summary>
    public IList<JsonElement>? Permissions { get; set; }

    /// <summary>Tenant-specific translation overrides; merged with base at runtime.</summary>
    public Dictionary<string, Dictionary<string, string>>? Translations { get; set; }
}
