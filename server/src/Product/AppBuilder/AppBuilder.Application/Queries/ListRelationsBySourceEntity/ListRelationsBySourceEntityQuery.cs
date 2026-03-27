using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListRelationsBySourceEntity;

public sealed record ListRelationsBySourceEntityQuery(Guid SourceEntityId)
    : IApplicationRequest<Result<List<RelationDefinitionDto>>>;
