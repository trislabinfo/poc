using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.ListReleasesByApplication;

public sealed record ListReleasesByApplicationQuery(Guid AppDefinitionId)
    : IApplicationRequest<Result<List<ApplicationReleaseDto>>>;
