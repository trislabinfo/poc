using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;

namespace AppBuilder.Application.Queries.GetReleaseInitialViewHtml;

public sealed record GetReleaseInitialViewHtmlQuery(Guid ReleaseId) : IApplicationRequest<Result<string?>>;
