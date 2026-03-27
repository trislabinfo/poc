namespace BuildingBlocks.Application.RequestDispatch;

/// <summary>
/// Abstraction for a request wrapped in a pipeline (e.g. for behaviors). Used by the messaging capability.
/// </summary>
public interface IPipelineRequest<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    TRequest InnerRequest { get; }
}
