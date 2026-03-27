using Capabilities.DatabaseSchema.Abstractions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Capabilities.DatabaseSchema.EfCore;

/// <summary>Reads database schema from a PostgreSQL database.</summary>
public sealed class EfCoreDatabaseSchemaReader : IDatabaseSchemaReader
{
    private readonly ILogger<EfCoreDatabaseSchemaReader> _logger;

    public EfCoreDatabaseSchemaReader(ILogger<EfCoreDatabaseSchemaReader> logger)
    {
        _logger = logger;
    }

    public async Task<Models.DatabaseSchema> ReadSchemaAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        var schema = new Models.DatabaseSchema();

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        // Read tables
        var tablesQuery = @"
            SELECT 
                t.table_name,
                obj_description(c.oid, 'pg_class') as table_comment
            FROM information_schema.tables t
            LEFT JOIN pg_class c ON c.relname = t.table_name
            WHERE t.table_schema = 'public'
              AND t.table_type = 'BASE TABLE'
              AND t.table_name NOT LIKE 'pg_%'
              AND t.table_name NOT LIKE '_efmigrations%'
            ORDER BY t.table_name";

        await using var tablesCommand = new NpgsqlCommand(tablesQuery, connection);
        await using var tablesReader = await tablesCommand.ExecuteReaderAsync(cancellationToken);

        var tableNames = new List<string>();
        while (await tablesReader.ReadAsync(cancellationToken))
        {
            tableNames.Add(tablesReader.GetString(0));
        }
        await tablesReader.CloseAsync();

        // Read columns for each table
        foreach (var tableName in tableNames)
        {
            var table = new Models.TableSchema
            {
                Name = tableName,
                DisplayName = tableName
            };

            var columnsQuery = @"
                SELECT 
                    c.column_name,
                    c.data_type,
                    c.is_nullable,
                    c.column_default,
                    c.ordinal_position,
                    CASE WHEN pk.column_name IS NOT NULL THEN true ELSE false END as is_primary_key
                FROM information_schema.columns c
                LEFT JOIN (
                    SELECT ku.table_name, ku.column_name
                    FROM information_schema.table_constraints tc
                    JOIN information_schema.key_column_usage ku
                        ON tc.constraint_name = ku.constraint_name
                    WHERE tc.constraint_type = 'PRIMARY KEY'
                ) pk ON c.table_name = pk.table_name AND c.column_name = pk.column_name
                WHERE c.table_schema = 'public'
                  AND c.table_name = @table_name
                ORDER BY c.ordinal_position";

            await using var columnsCommand = new NpgsqlCommand(columnsQuery, connection);
            columnsCommand.Parameters.AddWithValue("table_name", tableName);
            await using var columnsReader = await columnsCommand.ExecuteReaderAsync(cancellationToken);

            while (await columnsReader.ReadAsync(cancellationToken))
            {
                var columnName = columnsReader.GetString(0);
                var dataType = MapPostgresTypeToSql(columnsReader.GetString(1));
                var isNullable = columnsReader.GetString(2) == "YES";
                var defaultValue = columnsReader.IsDBNull(3) ? null : columnsReader.GetString(3);
                var ordinalPosition = columnsReader.GetInt32(4);
                var isPrimaryKey = columnsReader.GetBoolean(5);

                var column = new Models.ColumnSchema
                {
                    Name = columnName,
                    DisplayName = columnName,
                    SqlDataType = dataType,
                    IsNullable = isNullable,
                    DefaultValue = defaultValue,
                    Order = ordinalPosition,
                    IsPrimaryKey = isPrimaryKey
                };

                table.Columns.Add(column);

                if (isPrimaryKey)
                    table.PrimaryKeyColumnName = columnName;
            }
            await columnsReader.CloseAsync();

            schema.Tables.Add(table);
        }

        // Read foreign keys
        var fksQuery = @"
            SELECT
                tc.constraint_name,
                tc.table_name as source_table,
                kcu.column_name as source_column,
                ccu.table_name AS target_table,
                ccu.column_name AS target_column,
                rc.delete_rule
            FROM information_schema.table_constraints AS tc
            JOIN information_schema.key_column_usage AS kcu
                ON tc.constraint_name = kcu.constraint_name
            JOIN information_schema.constraint_column_usage AS ccu
                ON ccu.constraint_name = tc.constraint_name
            LEFT JOIN information_schema.referential_constraints AS rc
                ON tc.constraint_name = rc.constraint_name
            WHERE tc.constraint_type = 'FOREIGN KEY'
              AND tc.table_schema = 'public'
            ORDER BY tc.table_name, tc.constraint_name";

        await using var fksCommand = new NpgsqlCommand(fksQuery, connection);
        await using var fksReader = await fksCommand.ExecuteReaderAsync(cancellationToken);

        while (await fksReader.ReadAsync(cancellationToken))
        {
            var fk = new Models.ForeignKeySchema
            {
                Name = fksReader.GetString(0),
                SourceTableName = fksReader.GetString(1),
                SourceColumnName = fksReader.GetString(2),
                TargetTableName = fksReader.GetString(3),
                TargetColumnName = fksReader.GetString(4),
                CascadeDelete = fksReader.GetString(5) == "CASCADE"
            };

            schema.ForeignKeys.Add(fk);
        }

        return schema;
    }

    private static string MapPostgresTypeToSql(string postgresType)
    {
        return postgresType.ToLowerInvariant() switch
        {
            "character varying" or "varchar" => "text",
            "character" or "char" => "text",
            "text" => "text",
            "integer" or "int4" => "integer",
            "bigint" or "int8" => "bigint",
            "smallint" or "int2" => "smallint",
            "numeric" or "decimal" => "numeric",
            "double precision" or "float8" => "double precision",
            "real" or "float4" => "real",
            "boolean" or "bool" => "boolean",
            "timestamp with time zone" or "timestamptz" => "timestamp with time zone",
            "timestamp without time zone" or "timestamp" => "timestamp",
            "date" => "date",
            "time without time zone" or "time" => "time",
            "time with time zone" or "timetz" => "time with time zone",
            "uuid" => "uuid",
            "jsonb" => "jsonb",
            "json" => "json",
            _ => postgresType
        };
    }
}
