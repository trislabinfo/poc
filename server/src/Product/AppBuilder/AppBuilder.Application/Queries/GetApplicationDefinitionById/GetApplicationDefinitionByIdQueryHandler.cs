using AppBuilder.Application.DTOs;
using AppBuilder.Application.Mappers;
using AppBuilder.Domain.Repositories;
using BuildingBlocks.Application.Handlers;

namespace AppBuilder.Application.Queries.GetAppDefinitionById;

public sealed class GetAppDefinitionByIdQueryHandler
    : BaseGetByIdQueryHandler<AppDefinition.Domain.Entities.Application.AppDefinition, Guid, AppDefinitionDto, GetAppDefinitionByIdQuery>
{
    public GetAppDefinitionByIdQueryHandler(IAppDefinitionRepository repository)
        : base(repository)
    {
    }

    protected override string NotFoundCode => "AppBuilder.AppDefinition.NotFound";
    protected override string NotFoundMessage => "Application definition not found.";

    protected override Guid GetIdFromQuery(GetAppDefinitionByIdQuery query) => query.AppDefinitionId;

    protected override AppDefinitionDto MapToResponse(AppDefinition.Domain.Entities.Application.AppDefinition entity) =>
        AppDefinitionMapper.ToDto(entity);
}
