using PlatformMetaModel.Breakpoint;
using PlatformMetaModel.Common;
using PlatformMetaModel.Extension;
using PlatformMetaModel.Theme;

namespace PlatformMetaModel.Application;

/// <summary>
/// Application definition (root container). Contains entities and other definitions.
/// </summary>
public class ApplicationDefinition : BaseApplicationDefinition
{
    /// <summary>Unique identifier for the app.</summary>
    public required string Id { get; set; }

    /// <summary>Human-readable name.</summary>
    public required string Name { get; set; }

    ///// <summary>Semantic version: major.minor.patch with optional prerelease and build.</summary>
    //public required string Version { get; set; }

    //capability 
    //send notification - email, sms, push

    //featureflag TBD

    /// <summary>Extensions authored in this application; released to the extension catalog.</summary>
    public IList<ExtensionDefinition>? ExtensionDefinitions { get; set; }

    /// <summary>References to extensions from the extension catalog. Resolved at load time.</summary>
    public IList<ExtensionReference>? ExtensionReferences { get; set; }

    /// <summary>Theme definitions.</summary>
    public IList<ThemeDefinition>? Themes { get; set; }

    /// <summary>Optional. When present, overrides platform-level breakpoints for this application.</summary>
    public IList<BreakpointDefinition>? Breakpoint { get; set; }
}
