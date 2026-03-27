namespace AppBuilder.Application;

/// <summary>
/// Marker for commands that run in a transaction on the AppBuilder module's DbContext.
/// Used by <see cref="Behaviors.AppBuilderTransactionBehavior"/> to select the correct unit of work.
/// </summary>
public interface IAppBuilderCommand
{
}
