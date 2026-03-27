using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetReleaseEntityViewHtml;

public sealed record GetReleaseEntityViewHtmlQuery(
    Guid ReleaseId,
    Guid EntityId,
    string ViewType) : IApplicationRequest<Result<string?>>;
