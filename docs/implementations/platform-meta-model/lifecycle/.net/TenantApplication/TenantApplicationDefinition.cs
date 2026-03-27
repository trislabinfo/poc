using System.Text.Json.Serialization;

namespace PlatformMetaModel.Lifecycle.TenantApplication;

/// <summary>
/// Tenant application: a tenant's instance of an application, either installed from catalog,
/// created from scratch, or modified from an installed base.
/// </summary>
public class TenantApplicationDefinition
{
    /// <summary>Unique tenant application identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Tenant that owns this application instance.</summary>
    public required string TenantId { get; set; }

    /// <summary>Base application id (when source is catalog or when created from scratch referencing an app).</summary>
    public string? ApplicationId { get; set; }

    /// <summary>Base application release id (when installed from catalog or when tenant app is based on a release).</summary>
    public string? ApplicationReleaseId { get; set; }

    /// <summary>Catalog entry id when installed from catalog.</summary>
    public string? CatalogEntryId { get; set; }

    /// <summary>How this tenant application was created: from catalog or from scratch.</summary>
    [JsonPropertyName("source")]
    public required TenantApplicationSource Source { get; set; }

    /// <summary>Current status of the tenant application.</summary>
    [JsonPropertyName("status")]
    public required TenantApplicationStatus Status { get; set; }

    /// <summary>Tenant customizations applied on top of the base release.</summary>
    public TenantApplicationOverridesDefinition? Overrides { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantApplicationSource
{
    Catalog,
    FromScratch
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TenantApplicationStatus
{
    Draft,
    Active,
    Archived
}
