using AppDefinition.Contracts.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Queries.GetReleaseSnapshot;

public sealed record GetReleaseSnapshotQuery(Guid ReleaseId) : IApplicationRequest<Result<ApplicationSnapshotDto>>;
