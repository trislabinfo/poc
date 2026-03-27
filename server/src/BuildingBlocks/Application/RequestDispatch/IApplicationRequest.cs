namespace BuildingBlocks.Application.RequestDispatch;

/// <summary>
/// Marker for a request that returns a response of type <typeparamref name="TResponse"/>.
/// Implemented by all commands and queries. Enables dispatch without depending on any vendor message library.
/// </summary>
public interface IApplicationRequest<out TResponse>
{
}
