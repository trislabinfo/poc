using System.Text.Json.Serialization;

namespace PlatformMetaModel.Persistence;

/// <summary>Persistence/database configuration for the application.</summary>
public class PersistenceDefinition
{
    [JsonPropertyName("databaseProvider")]
    public DatabaseProvider DatabaseProvider { get; set; } = DatabaseProvider.Postgres;

    [JsonPropertyName("namingStrategy")]
    public NamingStrategy NamingStrategy { get; set; } = NamingStrategy.SnakeCase;

    /// <summary>Entity-to-table mapping and options.</summary>
    public IList<PersistenceEntityMapping>? Entities { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DatabaseProvider
{
    Postgres,
    Sqlserver,
    Mysql
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum NamingStrategy
{
    SnakeCase,
    PascalCase,
    CamelCase
}

/// <summary>Maps an entity to a table with optional indexes and column overrides.</summary>
public class PersistenceEntityMapping
{
    /// <summary>Entity id.</summary>
    public required string Entity { get; set; }

    /// <summary>Table name.</summary>
    public required string TableName { get; set; }

    /// <summary>Index definitions.</summary>
    public IList<PersistenceIndex>? Indexes { get; set; }

    /// <summary>Property-to-column overrides (property name -> column name).</summary>
    public Dictionary<string, string>? Properties { get; set; }
}

/// <summary>Index on a persistence table.</summary>
public class PersistenceIndex
{
    public required IList<string> Properties { get; set; }

    public bool Unique { get; set; }
}
