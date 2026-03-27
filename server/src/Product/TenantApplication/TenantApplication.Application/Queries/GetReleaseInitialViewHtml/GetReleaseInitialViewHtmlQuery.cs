using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Queries.GetReleaseInitialViewHtml;

public sealed record GetReleaseInitialViewHtmlQuery(Guid ReleaseId) : IApplicationRequest<Result<string?>>;
