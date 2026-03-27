namespace BuildingBlocks.Application.RequestDispatch;

/// <summary>
/// Handles a request and returns a response. Implemented by command/query handlers.
/// The dispatch implementation discovers and invokes these handlers.
/// </summary>
public interface IApplicationRequestHandler<in TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}
