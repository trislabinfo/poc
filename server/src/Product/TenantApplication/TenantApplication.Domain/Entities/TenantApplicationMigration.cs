using BuildingBlocks.Kernel.Domain;
using BuildingBlocks.Kernel.Results;
using TenantApplication.Domain.Enums;

namespace TenantApplication.Domain.Entities;

/// <summary>Schema migration between releases for a tenant application environment.</summary>
public sealed class TenantApplicationMigration : Entity<Guid>
{
    public Guid TenantApplicationEnvironmentId { get; private set; }
    public Guid? FromReleaseId { get; private set; }
    public Guid ToReleaseId { get; private set; }
    public string MigrationScriptJson { get; private set; } = "{}";
    public MigrationStatus Status { get; private set; }
    public DateTime? ExecutedAt { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }

    private TenantApplicationMigration() { }

    public static Result<TenantApplicationMigration> Create(
        Guid tenantApplicationEnvironmentId,
        Guid? fromReleaseId,
        Guid toReleaseId,
        string migrationScriptJson)
    {
        if (tenantApplicationEnvironmentId == Guid.Empty)
            return Result<TenantApplicationMigration>.Failure(
                Error.Validation("TenantApplicationMigration.EnvironmentId", "Environment ID is required."));
        if (toReleaseId == Guid.Empty)
            return Result<TenantApplicationMigration>.Failure(
                Error.Validation("TenantApplicationMigration.ToReleaseId", "Target release ID is required."));

        return Result<TenantApplicationMigration>.Success(new TenantApplicationMigration
        {
            Id = Guid.NewGuid(),
            TenantApplicationEnvironmentId = tenantApplicationEnvironmentId,
            FromReleaseId = fromReleaseId,
            ToReleaseId = toReleaseId,
            MigrationScriptJson = migrationScriptJson ?? "{}",
            Status = MigrationStatus.Pending
        });
    }

    public void MarkCompleted(IDateTimeProvider dateTimeProvider)
    {
        Status = MigrationStatus.Completed;
        ExecutedAt = dateTimeProvider.UtcNow;
        ErrorMessage = null;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    public void MarkFailed(string errorMessage, IDateTimeProvider dateTimeProvider)
    {
        Status = MigrationStatus.Failed;
        ExecutedAt = dateTimeProvider.UtcNow;
        ErrorMessage = errorMessage;
        UpdatedAt = dateTimeProvider.UtcNow;
    }

    public void UpdateScript(string migrationScriptJson)
    {
        if (Status != MigrationStatus.Pending)
            throw new InvalidOperationException("Only pending migrations can be updated.");
        MigrationScriptJson = migrationScriptJson ?? "{}";
    }

    public Result Approve(Guid approvedBy, IDateTimeProvider dateTimeProvider)
    {
        if (Status != MigrationStatus.Pending)
            return Result.Failure(Error.Validation("TenantApplicationMigration.Status", "Only pending migrations can be approved."));

        Status = MigrationStatus.Approved;
        ApprovedAt = dateTimeProvider.UtcNow;
        ApprovedBy = approvedBy;
        UpdatedAt = dateTimeProvider.UtcNow;
        return Result.Success();
    }

    public void MarkExecuting(IDateTimeProvider dateTimeProvider)
    {
        if (Status != MigrationStatus.Approved)
            throw new InvalidOperationException("Only approved migrations can be executed.");
        Status = MigrationStatus.Executing;
        UpdatedAt = dateTimeProvider.UtcNow;
    }
}
