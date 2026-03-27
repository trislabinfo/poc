using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.UpdateEnvironmentConfiguration;

public sealed class UpdateEnvironmentConfigurationCommandHandler
    : IApplicationRequestHandler<UpdateEnvironmentConfigurationCommand, Result>
{
    private readonly ITenantApplicationEnvironmentRepository _repository;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateEnvironmentConfigurationCommandHandler(
        ITenantApplicationEnvironmentRepository repository,
        ITenantApplicationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(UpdateEnvironmentConfigurationCommand request, CancellationToken cancellationToken)
    {
        var env = await _repository.GetByIdAsync(request.EnvironmentId, cancellationToken);
        if (env == null)
            return Result.Failure(Error.NotFound("TenantApplication.EnvironmentNotFound", "Environment not found."));
        env.UpdateConfiguration(request.ConfigurationJson, _dateTimeProvider);
        _repository.Update(env);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
