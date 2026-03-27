using AppBuilder.Application.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetAppDefinitionById;

public sealed record GetAppDefinitionByIdQuery(Guid AppDefinitionId)
    : IApplicationRequest<Result<AppDefinitionDto>>;
