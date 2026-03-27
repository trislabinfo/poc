namespace AppBuilder.McpServer;

public sealed class AppBuilderApiClientException(
    string message,
    System.Net.HttpStatusCode statusCode)
    : Exception(message)
{
    public System.Net.HttpStatusCode StatusCode { get; } = statusCode;
}

