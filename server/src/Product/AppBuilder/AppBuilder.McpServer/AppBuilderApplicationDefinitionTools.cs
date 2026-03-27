using System.ComponentModel;
using AppBuilder.Application.DTOs;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace AppBuilder.McpServer;

[McpServerToolType]
public static class AppBuilderApplicationDefinitionTools
{
    [McpServerTool(Name = "application_definitions.create")]
    public static async Task<AppDefinitionDto> CreateApplicationDefinitionAsync(
        [Description("Application definition name")] string name,
        [Description("Application definition description")] string description,
        [Description("Slug (unique, normalized server-side)")] string slug,
        [Description("Whether the application is public")] bool isPublic,
        AppBuilderApiClient apiClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new CreateApplicationDefinitionRequest(name, description, slug, isPublic);
            return await apiClient.CreateApplicationDefinitionAsync(request, cancellationToken);
        }
        catch (AppBuilderApiClientException ex)
        {
            // Report tool execution error to the MCP client so the model can self-correct.
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "application_definitions.get", ReadOnly = true)]
    public static async Task<AppDefinitionDto> GetApplicationDefinitionAsync(
        [Description("Application definition id")] Guid id,
        AppBuilderApiClient apiClient,
        CancellationToken cancellationToken)
    {
        try
        {
            return await apiClient.GetApplicationDefinitionAsync(id, cancellationToken);
        }
        catch (AppBuilderApiClientException ex)
        {
            throw new McpException(ex.Message);
        }
    }

    [McpServerTool(Name = "application_definitions.update")]
    public static async Task<AppDefinitionDto> UpdateApplicationDefinitionAsync(
        [Description("Application definition id")] Guid id,
        [Description("Updated application name")] string name,
        [Description("Updated application description")] string description,
        AppBuilderApiClient apiClient,
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new UpdateApplicationDefinitionRequest(name, description);
            return await apiClient.UpdateApplicationDefinitionAsync(id, request, cancellationToken);
        }
        catch (AppBuilderApiClientException ex)
        {
            throw new McpException(ex.Message);
        }
    }
}

