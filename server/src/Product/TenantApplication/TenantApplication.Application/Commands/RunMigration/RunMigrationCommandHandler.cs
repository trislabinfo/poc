using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Application.MigrationExecution;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.RunMigration;

public sealed class RunMigrationCommandHandler
    : IApplicationRequestHandler<RunMigrationCommand, Result>
{
    private readonly ITenantApplicationMigrationRepository _migrationRepository;
    private readonly IMigrationExecutor _migrationExecutor;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;

    public RunMigrationCommandHandler(
        ITenantApplicationMigrationRepository migrationRepository,
        IMigrationExecutor migrationExecutor,
        ITenantApplicationUnitOfWork unitOfWork)
    {
        _migrationRepository = migrationRepository;
        _migrationExecutor = migrationExecutor;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(RunMigrationCommand request, CancellationToken cancellationToken)
    {
        var migration = await _migrationRepository.GetByIdAsync(request.MigrationId, cancellationToken);
        if (migration == null)
            return Result.Failure(Error.NotFound("TenantApplication.MigrationNotFound", "Migration not found."));

        if (migration.TenantApplicationEnvironmentId != request.EnvironmentId)
            return Result.Failure(Error.Validation("TenantApplication.MigrationMismatch", "Migration does not belong to the specified environment."));

        // Execute migration (this will update status internally)
        var result = await _migrationExecutor.ExecuteMigrationAsync(request.MigrationId, cancellationToken);

        if (result.IsSuccess)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
