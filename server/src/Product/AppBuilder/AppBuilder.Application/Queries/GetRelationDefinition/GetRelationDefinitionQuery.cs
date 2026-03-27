using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetRelationDefinition;

public sealed record GetRelationDefinitionQuery(Guid RelationId) : IApplicationRequest<Result<RelationDefinitionDto>>;
