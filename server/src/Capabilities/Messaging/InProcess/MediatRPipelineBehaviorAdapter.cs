using BuildingBlocks.Application.RequestDispatch;
using MediatR;

namespace Capabilities.Messaging.InProcess;

/// <summary>
/// Adapts our <see cref="IRequestPipelineBehavior{TRequest,TResponse}"/> chain to MediatR's <see cref="IPipelineBehavior{TRequest,TResponse}"/>.
/// Composes all registered behaviors and runs them in order; the final "next" invokes the handler.
/// </summary>
internal sealed class MediatRPipelineBehaviorAdapter<TRequest, TResponse> : IPipelineBehavior<RequestEnvelope<TRequest, TResponse>, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IEnumerable<IRequestPipelineBehavior<TRequest, TResponse>> _behaviors;

    public MediatRPipelineBehaviorAdapter(IEnumerable<IRequestPipelineBehavior<TRequest, TResponse>> behaviors)
    {
        _behaviors = behaviors;
    }

    public async Task<TResponse> Handle(
        RequestEnvelope<TRequest, TResponse> request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var inner = request.InnerRequest;
        var behaviors = _behaviors.ToList();
        if (behaviors.Count == 0)
            return await next();

        async Task<TResponse> RunPipeline(int index)
        {
            if (index >= behaviors.Count)
                return await next();
            return await behaviors[index].HandleAsync(inner, ct => RunPipeline(index + 1), cancellationToken);
        }

        return await RunPipeline(0);
    }
}
