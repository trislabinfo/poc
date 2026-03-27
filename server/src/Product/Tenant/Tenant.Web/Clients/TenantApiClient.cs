using BuildingBlocks.Web.Rest;
using Tenant.Web.Models;

namespace Tenant.Web.Clients;

public sealed class TenantApiClient : RestApiClientBase, ITenantApiClient
{
    public TenantApiClient(HttpClient httpClient) : base(httpClient)
    {
    }

    public Task<RestCallResult<TenantDto>> CreateTenantAsync(
        CreateTenantRequest request,
        CancellationToken cancellationToken = default)
    {
        return PostAsync<CreateTenantRequest, TenantDto>("api/tenant", request, cancellationToken);
    }
}

