using System.Text.Json.Serialization;

namespace PlatformMetaModel.DataSource;

/// <summary>
/// Data source (REST, gRPC, database). Operations are defined inside; each has an id (operationId) referenced by layout nodes.
/// </summary>
public class DataSourceDefinition
{
    /// <summary>Unique data source identifier.</summary>
    public required string Id { get; set; }

    public string? DisplayName { get; set; }

    [JsonPropertyName("type")]
    public required DataSourceType Type { get; set; }

    public DataSourceAuthenticationDefinition? Authentication { get; set; }

    /// <summary>Type-specific: baseUrl for rest, address for grpc, provider/connectionString for database.</summary>
    public Dictionary<string, object>? Connection { get; set; }

    /// <summary>Operations defined inside this data source; each has an id (operationId) used by layout nodes.</summary>
    public IList<DataSourceOperationDefinition>? Operation { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataSourceType
{
    Rest,
    Grpc,
    Database
}

/// <summary>Authentication for a data source.</summary>
public class DataSourceAuthenticationDefinition
{
    [JsonPropertyName("type")]
    public DataSourceAuthType? Type { get; set; }

    /// <summary>Type-specific config (headerName, token, username/password, tokenEndpoint, etc.).</summary>
    public Dictionary<string, object>? Config { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataSourceAuthType
{
    None,
    ApiKey,
    Bearer,
    Basic,
    Oauth2,
    ClientCertificate
}

/// <summary>
/// Operation defined inside a data source; shape depends on operation type (read/write/database).
/// </summary>
public class DataSourceOperationDefinition
{
    /// <summary>Operation id (operationId) referenced by layout nodes.</summary>
    public required string Id { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>For REST: list | autocomplete. For database: query | table.</summary>
    public string? Kind { get; set; }

    /// <summary>REST method: GET for read, POST/PUT/PATCH/DELETE for write.</summary>
    public string? Method { get; set; }

    public string? Path { get; set; }

    /// <summary>e.g. /search?q={searchTerm}</summary>
    public string? PathTemplate { get; set; }

    public ResponseMapping? ResponseMapping { get; set; }

    /// <summary>Database query or table name.</summary>
    public string? Query { get; set; }

    public string? Table { get; set; }
}

/// <summary>Response mapping for option label/value and items path.</summary>
public class ResponseMapping
{
    /// <summary>JSON path in response item for display (e.g. name).</summary>
    public string? OptionLabelPath { get; set; }

    /// <summary>JSON path in response item for value to store (e.g. id).</summary>
    public string? OptionValuePath { get; set; }

    /// <summary>JSON path to array in response, e.g. data.items.</summary>
    public string? ItemsPath { get; set; }
}
