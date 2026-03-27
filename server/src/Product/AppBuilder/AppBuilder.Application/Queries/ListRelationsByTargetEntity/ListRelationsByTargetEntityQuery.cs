using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListRelationsByTargetEntity;

public sealed record ListRelationsByTargetEntityQuery(Guid TargetEntityId)
    : IApplicationRequest<Result<List<RelationDefinitionDto>>>;
