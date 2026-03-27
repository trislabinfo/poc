using Capabilities.DatabaseSchema.Abstractions;
using Capabilities.DatabaseSchema.Models;

namespace Capabilities.DatabaseSchema.EfCore;

/// <summary>Generates PostgreSQL SQL migration scripts from schema change sets.</summary>
public sealed class EfCoreMigrationScriptGenerator : IMigrationScriptGenerator
{
    public Task<string> GenerateMigrationScriptAsync(
        Models.SchemaChangeSet changeSet,
        Models.DatabaseSchema targetSchema,
        CancellationToken cancellationToken = default)
    {
        var script = new System.Text.StringBuilder();
        script.AppendLine("-- Migration script generated automatically");
        script.AppendLine("-- DO NOT MODIFY MANUALLY unless you know what you're doing");
        script.AppendLine($"-- Generated At: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        script.AppendLine();

        var targetTables = targetSchema.Tables.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

        // Generate table creation statements with columns
        foreach (var tableChange in changeSet.TableChanges.Where(t => t.ChangeType == TableChangeType.Added))
        {
            if (!targetTables.TryGetValue(tableChange.TableName, out var tableSchema))
                continue;

            script.AppendLine($"-- Create table: {tableChange.TableName}");
            script.AppendLine($"CREATE TABLE IF NOT EXISTS \"{tableChange.TableName}\" (");
            var columnDefs = new List<string>();
            foreach (var column in tableSchema.Columns.OrderBy(c => c.Order))
            {
                var colDef = $"    \"{column.Name}\" {column.SqlDataType}";
                if (!column.IsNullable)
                    colDef += " NOT NULL";
                if (!string.IsNullOrWhiteSpace(column.DefaultValue))
                    colDef += $" DEFAULT {column.DefaultValue}";
                columnDefs.Add(colDef);
            }
            script.AppendLine(string.Join(",\n", columnDefs));
            script.AppendLine(");");
            script.AppendLine();
        }

        // Generate column changes
        foreach (var columnChange in changeSet.ColumnChanges)
        {
            switch (columnChange.ChangeType)
            {
                case ColumnChangeType.Added:
                    script.AppendLine($"-- Add column: {columnChange.TableName}.{columnChange.ColumnName}");
                    script.AppendLine($"ALTER TABLE \"{columnChange.TableName}\"");
                    script.Append($"    ADD COLUMN \"{columnChange.ColumnName}\" {columnChange.NewSqlDataType}");
                    if (columnChange.NewIsNullable == false)
                        script.Append(" NOT NULL");
                    script.AppendLine(";");
                    script.AppendLine();
                    break;

                case ColumnChangeType.Removed:
                    script.AppendLine($"-- Remove column: {columnChange.TableName}.{columnChange.ColumnName}");
                    script.AppendLine($"ALTER TABLE \"{columnChange.TableName}\"");
                    script.AppendLine($"    DROP COLUMN IF EXISTS \"{columnChange.ColumnName}\";");
                    script.AppendLine();
                    break;

                case ColumnChangeType.Modified:
                    script.AppendLine($"-- Modify column: {columnChange.TableName}.{columnChange.ColumnName}");
                    if (columnChange.NewSqlDataType != null)
                    {
                        script.AppendLine($"ALTER TABLE \"{columnChange.TableName}\"");
                        script.AppendLine($"    ALTER COLUMN \"{columnChange.ColumnName}\" TYPE {columnChange.NewSqlDataType};");
                        script.AppendLine();
                    }
                    if (columnChange.NewIsNullable.HasValue)
                    {
                        script.AppendLine($"ALTER TABLE \"{columnChange.TableName}\"");
                        script.AppendLine($"    ALTER COLUMN \"{columnChange.ColumnName}\" {(columnChange.NewIsNullable.Value ? "DROP" : "SET")} NOT NULL;");
                        script.AppendLine();
                    }
                    break;
            }
        }

        // Generate foreign key changes
        foreach (var fkChange in changeSet.ForeignKeyChanges)
        {
            switch (fkChange.ChangeType)
            {
                case ForeignKeyChangeType.Added:
                    // Find the FK details from target schema
                    var fk = targetSchema.ForeignKeys.FirstOrDefault(f =>
                        f.Name == fkChange.ForeignKeyName &&
                        f.SourceTableName == fkChange.SourceTableName);

                    if (fk != null)
                    {
                        script.AppendLine($"-- Add foreign key: {fkChange.ForeignKeyName}");
                        script.AppendLine($"ALTER TABLE \"{fkChange.SourceTableName}\"");
                        script.AppendLine($"    ADD CONSTRAINT \"{fkChange.ForeignKeyName}\"");
                        script.AppendLine($"    FOREIGN KEY (\"{fk.SourceColumnName}\")");
                        script.AppendLine($"    REFERENCES \"{fkChange.TargetTableName}\" (\"{fk.TargetColumnName}\")");
                        if (fk.CascadeDelete)
                            script.AppendLine("    ON DELETE CASCADE");
                        script.AppendLine(";");
                        script.AppendLine();
                    }
                    break;

                case ForeignKeyChangeType.Removed:
                    script.AppendLine($"-- Remove foreign key: {fkChange.ForeignKeyName}");
                    script.AppendLine($"ALTER TABLE \"{fkChange.SourceTableName}\"");
                    script.AppendLine($"    DROP CONSTRAINT IF EXISTS \"{fkChange.ForeignKeyName}\";");
                    script.AppendLine();
                    break;
            }
        }

        // Generate table removal statements (at the end, after FK removal)
        foreach (var tableChange in changeSet.TableChanges.Where(t => t.ChangeType == TableChangeType.Removed))
        {
            script.AppendLine($"-- Remove table: {tableChange.TableName}");
            script.AppendLine($"DROP TABLE IF EXISTS \"{tableChange.TableName}\" CASCADE;");
            script.AppendLine();
        }

        return Task.FromResult(script.ToString());
    }
}
