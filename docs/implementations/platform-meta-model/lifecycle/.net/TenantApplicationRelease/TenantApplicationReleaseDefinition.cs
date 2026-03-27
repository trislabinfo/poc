using PlatformMetaModel.Lifecycle.ApplicationRelease;
using PlatformMetaModel.Lifecycle.Common;

namespace PlatformMetaModel.Lifecycle.TenantApplicationRelease;

/// <summary>
/// Tenant application release: immutable effective snapshot at a version. Release metadata only;
/// content is stored as TenantApplicationReleaseArtifactDefinition rows. Used for deployment.
/// </summary>
public class TenantApplicationReleaseDefinition : AuditDefinition
{
    /// <summary>Unique tenant application release identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Tenant application this release belongs to.</summary>
    public required string TenantApplicationId { get; set; }

    /// <summary>Base application release this tenant release was built from (for traceability).</summary>
    public string? ApplicationReleaseId { get; set; }

    /// <summary>Semantic version: major.minor.patch with optional prerelease and build.</summary>
    public required string Version { get; set; }

    /// <summary>Validation status of the snapshot.</summary>
    public required ReleaseStatus Status { get; set; }

    /// <summary>Optional DDD aggregate version for optimistic concurrency.</summary>
    public DomainModelDefinition? DomainModel { get; set; }
}
