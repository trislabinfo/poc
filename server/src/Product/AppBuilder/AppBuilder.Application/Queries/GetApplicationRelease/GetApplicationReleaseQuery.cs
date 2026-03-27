using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetApplicationRelease;

public sealed record GetApplicationReleaseQuery(Guid ReleaseId) : IApplicationRequest<Result<ApplicationReleaseDto>>;
