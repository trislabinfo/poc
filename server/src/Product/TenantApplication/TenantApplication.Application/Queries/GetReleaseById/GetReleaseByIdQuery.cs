using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Queries.GetReleaseById;

public sealed record GetReleaseByIdQuery(Guid TenantApplicationId, Guid ReleaseId) : IApplicationRequest<Result<ApplicationReleaseDto>>;
