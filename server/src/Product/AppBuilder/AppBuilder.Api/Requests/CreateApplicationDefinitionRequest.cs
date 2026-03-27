namespace AppBuilder.Api.Requests;

public sealed record CreateAppDefinitionRequest(
    string Name,
    string Description,
    string Slug,
    bool IsPublic);
