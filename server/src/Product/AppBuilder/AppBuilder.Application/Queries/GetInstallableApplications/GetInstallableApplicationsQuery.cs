using AppBuilder.Application.DTOs;
using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetInstallableApplications;

/// <summary>Returns platform applications that are public and have at least one active release (available for tenant install).</summary>
public sealed record GetInstallableApplicationsQuery : IApplicationRequest<Result<List<InstallableApplicationDto>>>;
