namespace BuildingBlocks.Application.RequestDispatch;

/// <summary>
/// Represents a step in the request pipeline (e.g. validation, logging, transaction).
/// Implemented by the messaging capability and by modules; the dispatch implementation (e.g. MediatR) runs these in order.
/// Enables replacing the pipeline implementation without changing behavior logic.
/// </summary>
public interface IRequestPipelineBehavior<in TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    /// <summary>
    /// Handles the request, optionally calling <paramref name="next"/> to continue the pipeline.
    /// </summary>
    /// <param name="request">The application request.</param>
    /// <param name="next">Delegate to invoke the next step (next behavior or handler).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response from the pipeline.</returns>
    Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken = default);
}
