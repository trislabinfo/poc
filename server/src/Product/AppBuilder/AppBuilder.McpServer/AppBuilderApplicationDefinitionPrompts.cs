using System.ComponentModel;
using ModelContextProtocol.Server;

namespace AppBuilder.McpServer;

[McpServerPromptType]
public static class AppBuilderApplicationDefinitionPrompts
{
    [McpServerPrompt(Name = "application_definitions.create_workflow")]
    [Description("Workflow for creating an application definition using MCP tools.")]
    public static string CreateApplicationDefinitionWorkflow()
    {
        return """
You are helping a user build an application definition in the AppBuilder service.

Workflow:
1) Collect required fields: name, description, slug, isPublic.
2) Call `application_definitions.create` with those fields.
3) Call `application_definitions.get` with the returned id to confirm.
4) Present the created application definition back to the user.
""";
    }
}

