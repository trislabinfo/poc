namespace AppDefinition.Contracts.DTOs;

/// <summary>
/// Application structure snapshot for runtime (navigation, pages, data sources, entities).
/// Used by BFF and runtime client; aligned with client/packages/contracts ApplicationSnapshot.
/// </summary>
public sealed record ApplicationSnapshotDto(
    string NavigationJson,
    string PageJson,
    string DataSourceJson,
    string EntityJson,
    string? SchemaVersion = null);
