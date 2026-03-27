namespace AppBuilder.Application.DTOs;

/// <summary>Application with its active release, for tenant catalog / install.</summary>
public sealed record InstallableApplicationDto(
    Guid ApplicationId,
    string Name,
    string Slug,
    string? Description,
    Guid ActiveReleaseId,
    string Version,
    int Major,
    int Minor,
    int Patch,
    string? ReleaseNotes);
