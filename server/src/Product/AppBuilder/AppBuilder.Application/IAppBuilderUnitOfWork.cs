using AppBuilder.Domain.Repositories;
using AppDefinition.Domain.Repositories;
using BuildingBlocks.Application.UnitOfWork;

namespace AppBuilder.Application;

/// <summary>
/// Unit of work for the AppBuilder module's DbContext. Used by <see cref="Behaviors.AppBuilderTransactionBehavior"/>.
/// </summary>
public interface IAppBuilderUnitOfWork : IUnitOfWork
{
    IAppDefinitionRepository AppDefinitions { get; }
    IApplicationReleaseRepository ApplicationReleases { get; }
    IEntityDefinitionRepository EntityDefinitions { get; }
    IPropertyDefinitionRepository PropertyDefinitions { get; }
    IRelationDefinitionRepository RelationDefinitions { get; }
    INavigationDefinitionRepository NavigationDefinitions { get; }
    IPageDefinitionRepository PageDefinitions { get; }
    IDataSourceDefinitionRepository DataSourceDefinitions { get; }
    IReleaseEntityViewRepository ReleaseEntityViews { get; }
}
