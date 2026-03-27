using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Enums;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.UpdateMigrationScript;

public sealed class UpdateMigrationScriptCommandHandler
    : IApplicationRequestHandler<UpdateMigrationScriptCommand, Result>
{
    private readonly ITenantApplicationMigrationRepository _migrationRepository;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;

    public UpdateMigrationScriptCommandHandler(
        ITenantApplicationMigrationRepository migrationRepository,
        ITenantApplicationUnitOfWork unitOfWork)
    {
        _migrationRepository = migrationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> HandleAsync(UpdateMigrationScriptCommand request, CancellationToken cancellationToken)
    {
        var migration = await _migrationRepository.GetByIdAsync(request.MigrationId, cancellationToken);
        if (migration == null)
            return Result.Failure(Error.NotFound("TenantApplication.MigrationNotFound", "Migration not found."));

        if (migration.TenantApplicationEnvironmentId != request.EnvironmentId)
            return Result.Failure(Error.Validation("TenantApplication.MigrationMismatch", "Migration does not belong to the specified environment."));

        if (migration.Status != MigrationStatus.Pending)
            return Result.Failure(Error.Validation("TenantApplication.MigrationNotPending", "Only pending migrations can be updated."));

        // Update script (stored in MigrationScriptJson field)
        migration.UpdateScript(request.MigrationScriptJson);
        _migrationRepository.Update(migration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
