namespace AppBuilder.Web;

public interface IAppBuilderToolExecutor
{
    Task<string> ExecuteToolAsync(
        string toolName,
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default);
}

