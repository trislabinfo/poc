using Capabilities.DatabaseSchema.Abstractions;

namespace Capabilities.DatabaseSchema.EfCore;

/// <summary>Generates PostgreSQL DDL scripts from database schema.</summary>
public sealed class EfCoreDdlScriptGenerator : IDdlScriptGenerator
{
    public Task<Models.DdlScript> GenerateDdlScriptAsync(
        Models.DatabaseSchema schema,
        string version,
        CancellationToken cancellationToken = default)
    {
        var script = new Models.DdlScript
        {
            Version = version,
            GeneratedAt = DateTime.UtcNow
        };

        var createTablesScript = new System.Text.StringBuilder();
        var createIndexesScript = new System.Text.StringBuilder();
        var createForeignKeysScript = new System.Text.StringBuilder();
        var completeScript = new System.Text.StringBuilder();

        completeScript.AppendLine("-- DDL Script Generated Automatically");
        completeScript.AppendLine($"-- Version: {version}");
        completeScript.AppendLine($"-- Generated At: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        completeScript.AppendLine();

        // Generate CREATE TABLE statements
        createTablesScript.AppendLine("-- ============================================");
        createTablesScript.AppendLine("-- CREATE TABLES");
        createTablesScript.AppendLine("-- ============================================");
        createTablesScript.AppendLine();

        foreach (var table in schema.Tables.OrderBy(t => t.Name))
        {
            createTablesScript.AppendLine($"-- Table: {table.Name} ({table.DisplayName})");
            if (!string.IsNullOrWhiteSpace(table.Description))
                createTablesScript.AppendLine($"-- Description: {table.Description}");

            createTablesScript.AppendLine($"CREATE TABLE IF NOT EXISTS \"{table.Name}\" (");

            var columnDefs = new List<string>();
            foreach (var column in table.Columns.OrderBy(c => c.Order))
            {
                var colDef = $"    \"{column.Name}\" {column.SqlDataType}";
                if (!column.IsNullable)
                    colDef += " NOT NULL";
                if (!string.IsNullOrWhiteSpace(column.DefaultValue))
                    colDef += $" DEFAULT {column.DefaultValue}";
                columnDefs.Add(colDef);
            }

            createTablesScript.AppendLine(string.Join(",\n", columnDefs));
            createTablesScript.AppendLine(");");
            createTablesScript.AppendLine();
        }

        // Generate CREATE INDEX statements (for primary keys and potential indexes)
        createIndexesScript.AppendLine("-- ============================================");
        createIndexesScript.AppendLine("-- CREATE INDEXES");
        createIndexesScript.AppendLine("-- ============================================");
        createIndexesScript.AppendLine();

        foreach (var table in schema.Tables)
        {
            if (!string.IsNullOrWhiteSpace(table.PrimaryKeyColumnName))
            {
                createIndexesScript.AppendLine($"-- Primary key index for {table.Name}");
                createIndexesScript.AppendLine($"CREATE UNIQUE INDEX IF NOT EXISTS \"pk_{table.Name}_{table.PrimaryKeyColumnName}\"");
                createIndexesScript.AppendLine($"    ON \"{table.Name}\" (\"{table.PrimaryKeyColumnName}\");");
                createIndexesScript.AppendLine();
            }
        }

        // Generate FOREIGN KEY constraints
        createForeignKeysScript.AppendLine("-- ============================================");
        createForeignKeysScript.AppendLine("-- CREATE FOREIGN KEYS");
        createForeignKeysScript.AppendLine("-- ============================================");
        createForeignKeysScript.AppendLine();

        foreach (var fk in schema.ForeignKeys.OrderBy(f => f.SourceTableName))
        {
            createForeignKeysScript.AppendLine($"-- Foreign key: {fk.Name}");
            createForeignKeysScript.AppendLine($"ALTER TABLE \"{fk.SourceTableName}\" DROP CONSTRAINT IF EXISTS \"{fk.Name}\";");
            createForeignKeysScript.AppendLine($"ALTER TABLE \"{fk.SourceTableName}\"");
            createForeignKeysScript.AppendLine($"    ADD CONSTRAINT \"{fk.Name}\"");
            createForeignKeysScript.AppendLine($"    FOREIGN KEY (\"{fk.SourceColumnName}\")");
            createForeignKeysScript.AppendLine($"    REFERENCES \"{fk.TargetTableName}\" (\"{fk.TargetColumnName}\")");
            if (fk.CascadeDelete)
                createForeignKeysScript.AppendLine("    ON DELETE CASCADE");
            createForeignKeysScript.AppendLine(";");
            createForeignKeysScript.AppendLine();
        }

        // Combine all scripts
        script.CreateTablesScript = createTablesScript.ToString();
        script.CreateIndexesScript = createIndexesScript.ToString();
        script.CreateForeignKeysScript = createForeignKeysScript.ToString();

        completeScript.AppendLine(script.CreateTablesScript);
        completeScript.AppendLine(script.CreateIndexesScript);
        completeScript.AppendLine(script.CreateForeignKeysScript);

        script.CompleteScript = completeScript.ToString();

        return Task.FromResult(script);
    }
}
