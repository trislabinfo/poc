namespace AppBuilder.McpServer;

public sealed record CreateApplicationDefinitionRequest(
    string Name,
    string Description,
    string Slug,
    bool IsPublic);

public sealed record UpdateApplicationDefinitionRequest(
    string Name,
    string Description);

