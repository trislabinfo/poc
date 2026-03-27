using BuildingBlocks.Application.Behaviors;
using BuildingBlocks.Application.RequestDispatch;

namespace Identity.Application.Behaviors;

/// <summary>
/// Pipeline behavior that wraps Identity transactional commands in a database transaction.
/// Only runs when the request is <see cref="ITransactionalCommand"/> and <see cref="IIdentityCommand"/>.
/// </summary>
public sealed class IdentityTransactionBehavior<TRequest, TResponse> : IRequestPipelineBehavior<TRequest, TResponse>
    where TRequest : IApplicationRequest<TResponse>
{
    private readonly IIdentityUnitOfWork _unitOfWork;

    public IdentityTransactionBehavior(IIdentityUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<CancellationToken, Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (request is not ITransactionalCommand || request is not IIdentityCommand)
            return await next(cancellationToken);

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var response = await next(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
