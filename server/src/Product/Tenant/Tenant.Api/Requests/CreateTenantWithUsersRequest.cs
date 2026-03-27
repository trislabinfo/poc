namespace Tenant.Api.Requests;

public sealed record CreateTenantWithUsersRequest(
    string Name,
    string Slug,
    IReadOnlyList<UserDataRequest> Users);
