using PlatformMetaModel.Entity;
using PlatformMetaModel.Layout;

namespace PlatformMetaModel.Extension;

/// <summary>
/// Reference to an extension from the extension catalog. Resolved at load time; resolved extension content is merged
/// into the effective application definition with namespacing (extensionId.id). Optional overrides extend or override
/// extension entities and pages.
/// </summary>
public class ExtensionReference
{
    /// <summary>Extension identifier (e.g. employee, hunting). Must match the extension's id.</summary>
    public required string ExtensionId { get; set; }

    /// <summary>Semantic version or range (e.g. 1.0.0 or ^1.0.0).</summary>
    public required string Version { get; set; }

    /// <summary>Optional URL, file path, or registry key to resolve the extension JSON.</summary>
    public string? Source { get; set; }

    /// <summary>Optional entity and page overrides.</summary>
    public ExtensionReferenceOverrides? Overrides { get; set; }
}

/// <summary>Optional overrides applied when merging this extension.</summary>
public class ExtensionReferenceOverrides
{
    /// <summary>Additional properties to add to extension entities.</summary>
    public IList<ExtensionEntityPropertyAddition>? EntityPropertyAdditions { get; set; }

    /// <summary>Overrides for extension pages (fieldOverrides, listConfig).</summary>
    public IList<ExtensionPageOverride>? PageOverrides { get; set; }
}

/// <summary>
/// Additional properties to append to an extension entity. Entity id is the extension's local entity id.
/// </summary>
public class ExtensionEntityPropertyAddition
{
    /// <summary>Extension entity id (e.g. Employee). Must exist in the extension.</summary>
    public required string EntityId { get; set; }

    /// <summary>Property definitions to add to this entity.</summary>
    public required IList<PropertyDefinition> Properties { get; set; }
}

/// <summary>
/// Override or extend an extension page. Page id is the extension's local page id.
/// </summary>
public class ExtensionPageOverride
{
    /// <summary>Extension page id (e.g. EmployeeEdit). Must exist in the extension.</summary>
    public required string PageId { get; set; }

    /// <summary>Additional or overriding field overrides for this page.</summary>
    public IList<FieldOverride>? FieldOverrides { get; set; }

    /// <summary>List config override for list pages.</summary>
    public ListConfig? ListConfig { get; set; }
}
