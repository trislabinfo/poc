using BuildingBlocks.Application.Handlers;
using Tenant.Application.DTOs;
using Tenant.Application.Mappers;
using Tenant.Domain.Repositories;
using TenantEntity = Tenant.Domain.Entities.Tenant;

namespace Tenant.Application.Queries.GetTenantById;

public sealed class GetTenantByIdQueryHandler
    : BaseGetByIdQueryHandler<TenantEntity, Guid, TenantDto, GetTenantByIdQuery>
{
    public GetTenantByIdQueryHandler(ITenantRepository repository)
        : base(repository)
    {
    }

    protected override string NotFoundCode => "Tenant.NotFound";
    protected override string NotFoundMessage => "Tenant not found.";

    protected override Guid GetIdFromQuery(GetTenantByIdQuery query) => query.TenantId;

    protected override TenantDto MapToResponse(TenantEntity entity) => TenantMapper.ToDto(entity);
}
