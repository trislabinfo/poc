using BuildingBlocks.Application.RequestDispatch;
using MediatR;

namespace Capabilities.Messaging.InProcess;

/// <summary>
/// Request dispatcher implementation using MediatR. Wraps application requests in an envelope and sends through the pipeline.
/// </summary>
public sealed class MediatRRequestDispatcher : IRequestDispatcher
{
    private readonly IMediator _mediator;

    public MediatRRequestDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<TResponse> SendAsync<TResponse>(IApplicationRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var sendCore = typeof(MediatRRequestDispatcher).GetMethod(nameof(SendCore), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!
            .MakeGenericMethod(requestType, typeof(TResponse));
        var task = sendCore.Invoke(null, [_mediator, request, cancellationToken]);
        return await (Task<TResponse>)task!;
    }

    private static async Task<TResponse> SendCore<TRequest, TResponse>(IMediator mediator, TRequest request, CancellationToken cancellationToken)
        where TRequest : IApplicationRequest<TResponse>
    {
        var envelope = new RequestEnvelope<TRequest, TResponse>(request);
        return await mediator.Send(envelope, cancellationToken);
    }
}
