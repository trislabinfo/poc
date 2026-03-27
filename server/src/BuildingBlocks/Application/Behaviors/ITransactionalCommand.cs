namespace BuildingBlocks.Application.Behaviors;

/// <summary>
/// Marker interface for commands that require a database transaction.
/// Each module provides a transaction behavior (e.g. TenantTransactionBehavior) that runs when
/// the request is ITransactionalCommand and the module's command marker (e.g. ITenantCommand).
/// </summary>
public interface ITransactionalCommand
{
}
