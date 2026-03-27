using BuildingBlocks.Kernel.Results;
using Tenant.Contracts;
using Tenant.Contracts.Services;
using Tenant.Domain.Repositories;

namespace Tenant.Application.Services;

/// <summary>
/// In-process implementation of ITenantResolverService. Used when Tenant and caller (e.g. TenantApplication) run in the same process.
/// </summary>
public sealed class TenantResolverService : ITenantResolverService
{
    private readonly ITenantRepository _tenantRepository;

    public TenantResolverService(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<Result<TenantInfoDto?>> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetBySlugAsync(slug, cancellationToken);
        if (tenant == null)
            return Result<TenantInfoDto?>.Success(null);
        return Result<TenantInfoDto?>.Success(new TenantInfoDto(tenant.Id, tenant.Slug));
    }

    public async Task<Result<TenantInfoDto?>> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            return Result<TenantInfoDto?>.Success(null);
        return Result<TenantInfoDto?>.Success(new TenantInfoDto(tenant.Id, tenant.Slug));
    }
}
