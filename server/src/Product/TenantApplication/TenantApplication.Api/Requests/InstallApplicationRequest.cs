namespace TenantApplication.Api.Requests;

public sealed record InstallApplicationRequest(
    Guid ApplicationReleaseId,
    string Name,
    string Slug,
    string? ConfigurationJson = null);
