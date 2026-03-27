using PlatformMetaModel.Common;

namespace PlatformMetaModel.Extension;

/// <summary>
/// Application extension: reusable bundle of entities, pages, navigation, workflows, roles, permissions, etc.
/// Can be defined inside an application definition (extensionDefinitions) and released to the extension catalog.
/// </summary>
public class ExtensionDefinition : CommonPropertiesDefinition
{
    /// <summary>Unique extension identifier; used as namespace prefix when merged (e.g. employee).</summary>
    public required string Id { get; set; }

    /// <summary>Human-readable name.</summary>
    public required string Name { get; set; }

    /// <summary>Semantic version: major.minor.patch with optional prerelease and build.</summary>
    public required string Version { get; set; }

    /// <summary>Other extensions this extension references. Loaded first; cycles rejected.</summary>
    public IList<ExtensionDependency>? DependsOn { get; set; }
}
