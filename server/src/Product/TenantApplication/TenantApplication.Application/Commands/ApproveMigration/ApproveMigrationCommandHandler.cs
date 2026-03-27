using BuildingBlocks.Application.RequestDispatch;
using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Repositories;

namespace TenantApplication.Application.Commands.ApproveMigration;

public sealed class ApproveMigrationCommandHandler
    : IApplicationRequestHandler<ApproveMigrationCommand, Result>
{
    private readonly ITenantApplicationMigrationRepository _migrationRepository;
    private readonly ITenantApplicationUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ApproveMigrationCommandHandler(
        ITenantApplicationMigrationRepository migrationRepository,
        ITenantApplicationUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _migrationRepository = migrationRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> HandleAsync(ApproveMigrationCommand request, CancellationToken cancellationToken)
    {
        var migration = await _migrationRepository.GetByIdAsync(request.MigrationId, cancellationToken);
        if (migration == null)
            return Result.Failure(Error.NotFound("TenantApplication.MigrationNotFound", "Migration not found."));

        if (migration.TenantApplicationEnvironmentId != request.EnvironmentId)
            return Result.Failure(Error.Validation("TenantApplication.MigrationMismatch", "Migration does not belong to the specified environment."));

        var result = migration.Approve(request.ApprovedBy, _dateTimeProvider);
        if (result.IsFailure)
            return result;

        _migrationRepository.Update(migration);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
