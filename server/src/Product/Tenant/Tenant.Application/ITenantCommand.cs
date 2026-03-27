namespace Tenant.Application;

/// <summary>
/// Marker for commands that run in a transaction on the Tenant module's DbContext.
/// Used by <see cref="TenantTransactionBehavior"/> to select the correct unit of work.
/// </summary>
public interface ITenantCommand
{
}
