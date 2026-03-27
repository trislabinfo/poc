using BuildingBlocks.Application.RequestDispatch;
using MediatR;

namespace Capabilities.Messaging.InProcess;

internal sealed class BridgeRequestHandler<TRequest, TResponse> : IRequestHandler<RequestEnvelope<TRequest, TResponse>, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IApplicationRequestHandler<TRequest, TResponse> _handler;

    public BridgeRequestHandler(IApplicationRequestHandler<TRequest, TResponse> handler)
    {
        _handler = handler;
    }

    public Task<TResponse> Handle(RequestEnvelope<TRequest, TResponse> request, CancellationToken cancellationToken)
    {
        return _handler.HandleAsync(request.InnerRequest, cancellationToken);
    }
}
