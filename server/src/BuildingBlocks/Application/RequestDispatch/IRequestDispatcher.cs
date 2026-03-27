namespace BuildingBlocks.Application.RequestDispatch;

/// <summary>
/// Sends a request and returns the response. Abstraction over the underlying dispatch implementation.
/// </summary>
public interface IRequestDispatcher
{
    Task<TResponse> SendAsync<TResponse>(IApplicationRequest<TResponse> request, CancellationToken cancellationToken = default);
}
