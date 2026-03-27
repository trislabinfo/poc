using BuildingBlocks.Application.RequestDispatch;
using MediatR;

namespace Capabilities.Messaging.InProcess;

/// <summary>
/// Wraps an application request for the in-process MediatR pipeline. Used by the messaging capability and by module transaction behaviors.
/// </summary>
public sealed class RequestEnvelope<TRequest, TResponse> : IPipelineRequest<TRequest, TResponse>, IRequest<TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    public TRequest InnerRequest { get; }

    public RequestEnvelope(TRequest innerRequest)
    {
        InnerRequest = innerRequest;
    }
}
