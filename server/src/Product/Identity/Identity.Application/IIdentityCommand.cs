namespace Identity.Application;

/// <summary>
/// Marker for commands that run in a transaction on the Identity module's DbContext.
/// Used by <see cref="Behaviors.IdentityTransactionBehavior"/> to select the correct unit of work.
/// </summary>
public interface IIdentityCommand
{
}
