using System.Text.Json.Serialization;

namespace TenantApplication.Application.DTOs;

/// <summary>
/// Result of resolving an application by URL (tenant slug + app slug + environment).
/// Used by Runtime BFF; aligned with client ResolvedApplication.
/// ConnectionString is for server-side use only (e.g. Runtime API); not serialized to the client.
/// </summary>
public sealed record ResolvedApplicationDto(
    Guid TenantId,
    string TenantSlug,
    Guid TenantApplicationId,
    string AppSlug,
    Guid ApplicationReleaseId,
    string EnvironmentConfiguration,
    bool IsTenantRelease,
    [property: JsonIgnore] // Server-side only; never send to client
    string? ConnectionString = null);
