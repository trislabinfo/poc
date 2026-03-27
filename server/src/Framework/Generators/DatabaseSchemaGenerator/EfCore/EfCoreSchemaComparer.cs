using Capabilities.DatabaseSchema.Abstractions;
using Capabilities.DatabaseSchema.Models;
using Microsoft.Extensions.Logging;

namespace Capabilities.DatabaseSchema.EfCore;

/// <summary>Compares database schemas using EF Core's IMigrationsModelDiffer when possible, falls back to model comparison.</summary>
public sealed class EfCoreSchemaComparer : ISchemaComparer
{
    private readonly ILogger<EfCoreSchemaComparer> _logger;

    public EfCoreSchemaComparer(ILogger<EfCoreSchemaComparer> logger)
    {
        _logger = logger;
    }

    public Task<Models.SchemaChangeSet> CompareAsync(
        Models.DatabaseSchema sourceSchema,
        Models.DatabaseSchema targetSchema,
        CancellationToken cancellationToken = default)
    {
        // For now, use direct model comparison
        // TODO: Enhance to use IMigrationsModelDiffer by converting DatabaseSchema to IRelationalModel
        return Task.FromResult(CompareSchemasDirectly(sourceSchema, targetSchema));
    }

    public async Task<Models.SchemaChangeSet> CompareWithDatabaseAsync(
        string connectionString,
        Models.DatabaseSchema targetSchema,
        CancellationToken cancellationToken = default)
    {
        // Read actual database schema
        var readerLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EfCoreDatabaseSchemaReader>.Instance;
        var reader = new EfCoreDatabaseSchemaReader(readerLogger);
        var actualSchema = await reader.ReadSchemaAsync(connectionString, cancellationToken);

        // Compare actual schema with target schema
        return CompareSchemasDirectly(actualSchema, targetSchema);
    }

    private static Models.SchemaChangeSet CompareSchemasDirectly(
        Models.DatabaseSchema sourceSchema,
        Models.DatabaseSchema targetSchema)
    {
        var changeSet = new Models.SchemaChangeSet();
        var sourceTables = sourceSchema.Tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
        var targetTables = targetSchema.Tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

        // Find added and removed tables
        foreach (var toTable in targetSchema.Tables)
        {
            if (!sourceTables.ContainsKey(toTable.Name))
            {
                changeSet.TableChanges.Add(new TableChange
                {
                    ChangeType = TableChangeType.Added,
                    TableName = toTable.Name,
                    DisplayName = toTable.DisplayName
                });
            }
        }

        foreach (var fromTable in sourceSchema.Tables)
        {
            if (!targetTables.ContainsKey(fromTable.Name))
            {
                changeSet.TableChanges.Add(new TableChange
                {
                    ChangeType = TableChangeType.Removed,
                    TableName = fromTable.Name
                });
            }
        }

        // Compare columns for existing tables
        foreach (var toTable in targetSchema.Tables)
        {
            if (!sourceTables.TryGetValue(toTable.Name, out var fromTable))
                continue; // Table was added, columns will be handled by table creation

            var fromColumns = fromTable.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);
            var toColumns = toTable.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            // Find added columns
            foreach (var toColumn in toTable.Columns)
            {
                if (!fromColumns.ContainsKey(toColumn.Name))
                {
                    changeSet.ColumnChanges.Add(new ColumnChange
                    {
                        ChangeType = ColumnChangeType.Added,
                        TableName = toTable.Name,
                        ColumnName = toColumn.Name,
                        NewSqlDataType = toColumn.SqlDataType,
                        NewIsNullable = toColumn.IsNullable
                    });
                }
            }

            // Find removed columns
            foreach (var fromColumn in fromTable.Columns)
            {
                if (!toColumns.ContainsKey(fromColumn.Name))
                {
                    changeSet.ColumnChanges.Add(new ColumnChange
                    {
                        ChangeType = ColumnChangeType.Removed,
                        TableName = fromTable.Name,
                        ColumnName = fromColumn.Name
                    });
                }
            }

            // Find modified columns
            foreach (var toColumn in toTable.Columns)
            {
                if (fromColumns.TryGetValue(toColumn.Name, out var fromColumn))
                {
                    if (fromColumn.SqlDataType != toColumn.SqlDataType ||
                        fromColumn.IsNullable != toColumn.IsNullable)
                    {
                        changeSet.ColumnChanges.Add(new ColumnChange
                        {
                            ChangeType = ColumnChangeType.Modified,
                            TableName = toTable.Name,
                            ColumnName = toColumn.Name,
                            NewSqlDataType = toColumn.SqlDataType,
                            NewIsNullable = toColumn.IsNullable
                        });
                    }
                }
            }
        }

        // Compare foreign keys
        var fromFks = sourceSchema.ForeignKeys.ToDictionary(
            fk => $"{fk.SourceTableName}.{fk.Name}",
            StringComparer.OrdinalIgnoreCase);
        var toFks = targetSchema.ForeignKeys.ToDictionary(
            fk => $"{fk.SourceTableName}.{fk.Name}",
            StringComparer.OrdinalIgnoreCase);

        foreach (var toFk in targetSchema.ForeignKeys)
        {
            var key = $"{toFk.SourceTableName}.{toFk.Name}";
            if (!fromFks.ContainsKey(key))
            {
                changeSet.ForeignKeyChanges.Add(new ForeignKeyChange
                {
                    ChangeType = ForeignKeyChangeType.Added,
                    ForeignKeyName = toFk.Name,
                    SourceTableName = toFk.SourceTableName,
                    TargetTableName = toFk.TargetTableName
                });
            }
        }

        foreach (var fromFk in sourceSchema.ForeignKeys)
        {
            var key = $"{fromFk.SourceTableName}.{fromFk.Name}";
            if (!toFks.ContainsKey(key))
            {
                changeSet.ForeignKeyChanges.Add(new ForeignKeyChange
                {
                    ChangeType = ForeignKeyChangeType.Removed,
                    ForeignKeyName = fromFk.Name,
                    SourceTableName = fromFk.SourceTableName,
                    TargetTableName = fromFk.TargetTableName
                });
            }
        }

        return changeSet;
    }
}
