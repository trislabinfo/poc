using PlatformMetaModel.CodeTable;
using PlatformMetaModel.Component;
using PlatformMetaModel.DataSource;
using PlatformMetaModel.Entity;
using PlatformMetaModel.Navigation;
using PlatformMetaModel.Page;
using PlatformMetaModel.Permission;
using PlatformMetaModel.Role;
using PlatformMetaModel.Workflow;

namespace PlatformMetaModel.Common;

/// <summary>
/// Shared properties used by both ApplicationDefinition and ExtensionDefinition.
/// Entities, pages, navigation, workflows, roles, permissions, code tables, data sources, and locale/translation settings.
/// </summary>
public class CommonPropertiesDefinition
{
    /// <summary>Entity definitions (business data structures).</summary>
    public IList<EntityDefinition>? Entities { get; set; }

    /// <summary>Page definitions (list, edit, custom).</summary>
    public IList<PageDefinition>? Pages { get; set; }

    /// <summary>Navigation definitions (each defines a navigation type and its item tree).</summary>
    public IList<NavigationDefinition>? Navigation { get; set; }

    /// <summary>Workflow definitions (BPMN-aligned).</summary>
    public IList<WorkflowDefinition>? Workflows { get; set; }

    /// <summary>Role definitions.</summary>
    public IList<RoleDefinition>? Roles { get; set; }

    /// <summary>Permission definitions.</summary>
    public IList<PermissionDefinition>? Permissions { get; set; }

    /// <summary>Code table definitions (reference/lookup data).</summary>
    public IList<CodeTableDefinition>? CodeTables { get; set; }

    /// <summary>Data source definitions (REST, gRPC, database).</summary>
    public IList<DataSourceDefinition>? DataSources { get; set; }

    /// <summary>Component registry definitions (available UI components).</summary>
    public IList<ComponentDefinition>? Components { get; set; }

    /// <summary>Default locale for formatting and translations (e.g. en, sl).</summary>
    public string? DefaultLocale { get; set; }

    /// <summary>Optional list of supported locale codes.</summary>
    public IList<string>? SupportedLocales { get; set; }

    /// <summary>Per-locale translation maps. Property names are locale codes (e.g. en, sl).</summary>
    public Dictionary<string, Dictionary<string, string>>? Translations { get; set; }
}
