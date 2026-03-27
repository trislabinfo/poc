using PlatformMetaModel.Lifecycle.ApplicationCatalog;
using PlatformMetaModel.Lifecycle.ApplicationRelease;
using PlatformMetaModel.Lifecycle.Deploy;
using PlatformMetaModel.Lifecycle.Environment;
using PlatformMetaModel.Lifecycle.ExtensionCatalog;
using PlatformMetaModel.Lifecycle.ExtensionRelease;
using PlatformMetaModel.Lifecycle.TenantApplication;
using PlatformMetaModel.Lifecycle.TenantApplicationRelease;

namespace PlatformMetaModel.Lifecycle;

/// <summary>
/// Application lifecycle schema root. Use alongside the application meta model: definition content validates
/// against the application meta model; lifecycle artifacts validate against this schema.
/// </summary>
public class ApplicationLifecycleDefinition
{
    /// <summary>Single application release (platform).</summary>
    public ApplicationReleaseDefinition? ApplicationRelease { get; set; }

    /// <summary>Application catalog: installable applications (entries offered to tenants).</summary>
    public IList<ApplicationCatalogDefinition>? ApplicationCatalog { get; set; }

    /// <summary>Tenant application instance (installed from catalog, from scratch, or modified).</summary>
    public TenantApplicationDefinition? TenantApplication { get; set; }

    /// <summary>Tenant application release snapshot.</summary>
    public TenantApplicationReleaseDefinition? TenantApplicationRelease { get; set; }

    /// <summary>Tenant application environment (dev, staging, prod).</summary>
    public EnvironmentDefinition? Environment { get; set; }

    /// <summary>Deployment of a tenant app release to an environment.</summary>
    public DeployDefinition? Deploy { get; set; }

    /// <summary>Single extension release (created when releasing extensions from an application; added to extension catalog).</summary>
    public ExtensionReleaseDefinition? ExtensionRelease { get; set; }

    /// <summary>Extension catalog: entries offered for applications to reference (extensionReferences).</summary>
    public IList<ExtensionCatalogEntryDefinition>? ExtensionCatalog { get; set; }
}
