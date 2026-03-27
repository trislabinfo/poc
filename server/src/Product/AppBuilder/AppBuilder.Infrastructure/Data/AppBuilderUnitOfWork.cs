using AppBuilder.Application;
using AppBuilder.Domain.Repositories;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Infrastructure.Persistence;

namespace AppBuilder.Infrastructure.Data;

/// <summary>
/// Unit of work for AppBuilderDbContext. Used by <see cref="AppBuilder.Application.Behaviors.AppBuilderTransactionBehavior"/>.
/// </summary>
public sealed class AppBuilderUnitOfWork : UnitOfWork<AppBuilderDbContext>, IAppBuilderUnitOfWork
{
    public AppBuilderUnitOfWork(
        AppBuilderDbContext context,
        IAppDefinitionRepository AppDefinitions,
        IApplicationReleaseRepository applicationReleases,
        IEntityDefinitionRepository entityDefinitions,
        IPropertyDefinitionRepository propertyDefinitions,
        IRelationDefinitionRepository relationDefinitions,
        INavigationDefinitionRepository navigationDefinitions,
        IPageDefinitionRepository pageDefinitions,
        IDataSourceDefinitionRepository dataSourceDefinitions,
        IReleaseEntityViewRepository releaseEntityViews)
        : base(context)
    {
        AppDefinitions = AppDefinitions;
        ApplicationReleases = applicationReleases;
        EntityDefinitions = entityDefinitions;
        PropertyDefinitions = propertyDefinitions;
        RelationDefinitions = relationDefinitions;
        NavigationDefinitions = navigationDefinitions;
        PageDefinitions = pageDefinitions;
        DataSourceDefinitions = dataSourceDefinitions;
        ReleaseEntityViews = releaseEntityViews;
    }

    public IAppDefinitionRepository AppDefinitions { get; }
    public IApplicationReleaseRepository ApplicationReleases { get; }
    public IEntityDefinitionRepository EntityDefinitions { get; }
    public IPropertyDefinitionRepository PropertyDefinitions { get; }
    public IRelationDefinitionRepository RelationDefinitions { get; }
    public INavigationDefinitionRepository NavigationDefinitions { get; }
    public IPageDefinitionRepository PageDefinitions { get; }
    public IDataSourceDefinitionRepository DataSourceDefinitions { get; }
    public IReleaseEntityViewRepository ReleaseEntityViews { get; }
}
