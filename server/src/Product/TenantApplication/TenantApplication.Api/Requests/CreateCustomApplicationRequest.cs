namespace TenantApplication.Api.Requests;

public sealed record CreateCustomApplicationRequest(
    string Name,
    string Slug,
    string? Description = null);
