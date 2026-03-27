using BuildingBlocks.Web.Rest;
using Tenant.Web.Models;

namespace Tenant.Web.Clients;

public interface ITenantApiClient
{
    Task<RestCallResult<TenantDto>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
}

