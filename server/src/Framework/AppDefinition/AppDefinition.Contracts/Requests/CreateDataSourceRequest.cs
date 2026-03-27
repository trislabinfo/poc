using AppDefinition.Domain.Enums;

namespace AppDefinition.Contracts.Requests;

/// <summary>Shared request for creating a data source definition (AppBuilder and TenantApplication).</summary>
public sealed record CreateDataSourceRequest(
    Guid AppDefinitionId,
    string Name,
    DataSourceType Type,
    string ConfigurationJson = "{}");
