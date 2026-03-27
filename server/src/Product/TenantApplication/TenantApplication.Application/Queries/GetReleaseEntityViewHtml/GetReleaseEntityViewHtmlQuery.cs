using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace TenantApplication.Application.Queries.GetReleaseEntityViewHtml;

public sealed record GetReleaseEntityViewHtmlQuery(
    Guid ReleaseId,
    Guid EntityId,
    string ViewType) : IApplicationRequest<Result<string?>>;
