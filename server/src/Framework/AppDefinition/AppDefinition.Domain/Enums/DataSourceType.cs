namespace AppDefinition.Domain.Enums;

/// <summary>Type of data source definition (shared by AppBuilder and TenantApplication).</summary>
public enum DataSourceType
{
    Entity = 0,
    RestApi = 1,
    Database = 2,
    GraphQL = 3
}
