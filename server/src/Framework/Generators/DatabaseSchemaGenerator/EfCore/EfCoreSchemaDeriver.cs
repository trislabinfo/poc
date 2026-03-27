using AppDefinition.Domain.Entities.Application;
using AppDefinition.Domain.Enums;
using Capabilities.DatabaseSchema.Abstractions;
using Capabilities.DatabaseSchema.Models;

namespace Capabilities.DatabaseSchema.EfCore;

/// <summary>Derives database schema from entity/property/relation definitions using EF Core patterns.</summary>
public sealed class EfCoreSchemaDeriver : ISchemaDeriver
{
    public Task<Models.DatabaseSchema> DeriveSchemaAsync(
        IReadOnlyList<EntityDefinition> entities,
        Dictionary<Guid, List<PropertyDefinition>> propertiesByEntityId,
        IReadOnlyList<RelationDefinition> relations,
        CancellationToken cancellationToken = default)
    {
        var schema = new Models.DatabaseSchema();
        var entityMap = entities.ToDictionary(e => e.Id, e => e);

        // Derive tables from entities
        foreach (var entity in entities)
        {
            var table = new Models.TableSchema
            {
                Name = entity.Name,
                DisplayName = entity.DisplayName,
                Description = entity.Description
            };

            // Get properties for this entity
            if (propertiesByEntityId.TryGetValue(entity.Id, out var properties))
            {
                foreach (var property in properties.OrderBy(p => p.Order))
                {
                    var column = new Models.ColumnSchema
                    {
                        Name = property.Name,
                        DisplayName = property.DisplayName,
                        SqlDataType = MapPropertyDataTypeToSql(property.DataType),
                        IsNullable = !property.IsRequired,
                        DefaultValue = property.DefaultValue,
                        Order = property.Order
                    };

                    // Check if this is the primary key
                    if (!string.IsNullOrWhiteSpace(entity.PrimaryKey) &&
                        entity.PrimaryKey.Equals(property.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        column.IsPrimaryKey = true;
                        table.PrimaryKeyColumnName = column.Name;
                    }

                    table.Columns.Add(column);
                }
            }

            // If no primary key column was found, add a default Id column
            if (string.IsNullOrWhiteSpace(table.PrimaryKeyColumnName))
            {
                var idColumn = new Models.ColumnSchema
                {
                    Name = "id",
                    DisplayName = "Id",
                    SqlDataType = "uuid",
                    IsNullable = false,
                    IsPrimaryKey = true,
                    Order = 0
                };
                table.Columns.Insert(0, idColumn);
                table.PrimaryKeyColumnName = idColumn.Name;
            }

            schema.Tables.Add(table);
        }

        // Derive foreign keys from relations and ensure source tables have the FK column
        foreach (var relation in relations)
        {
            if (!entityMap.TryGetValue(relation.SourceEntityId, out var sourceEntity) ||
                !entityMap.TryGetValue(relation.TargetEntityId, out var targetEntity))
                continue;

            // For ManyToOne and OneToMany, create FK on the "many" side
            if (relation.RelationType == RelationType.ManyToOne)
            {
                var fkColumnName = $"{targetEntity.Name.ToLowerInvariant()}_id";
                var targetPkName = GetPrimaryKeyColumnName(targetEntity, propertiesByEntityId);
                EnsureForeignKeyColumn(schema, sourceEntity.Name, fkColumnName, targetEntity.Name, targetPkName);
                var fk = new ForeignKeySchema
                {
                    Name = relation.Name,
                    SourceTableName = sourceEntity.Name,
                    SourceColumnName = fkColumnName,
                    TargetTableName = targetEntity.Name,
                    TargetColumnName = targetPkName,
                    CascadeDelete = relation.CascadeDelete
                };
                schema.ForeignKeys.Add(fk);
            }
            else if (relation.RelationType == RelationType.OneToMany)
            {
                var fkColumnName = $"{sourceEntity.Name.ToLowerInvariant()}_id";
                var targetPkName = GetPrimaryKeyColumnName(sourceEntity, propertiesByEntityId);
                EnsureForeignKeyColumn(schema, targetEntity.Name, fkColumnName, sourceEntity.Name, targetPkName);
                var fk = new ForeignKeySchema
                {
                    Name = relation.Name,
                    SourceTableName = targetEntity.Name,
                    SourceColumnName = fkColumnName,
                    TargetTableName = sourceEntity.Name,
                    TargetColumnName = targetPkName,
                    CascadeDelete = relation.CascadeDelete
                };
                schema.ForeignKeys.Add(fk);
            }
            // ManyToMany would require junction table - skip for now
        }

        return Task.FromResult(schema);
    }

    private static string MapPropertyDataTypeToSql(PropertyDataType dataType)
    {
        return dataType switch
        {
            PropertyDataType.String => "text",
            PropertyDataType.Number => "numeric",
            PropertyDataType.Boolean => "boolean",
            PropertyDataType.DateTime => "timestamp with time zone",
            PropertyDataType.Date => "date",
            PropertyDataType.Time => "time",
            PropertyDataType.Json => "jsonb",
            _ => "text"
        };
    }

    private static string GetPrimaryKeyColumnName(
        EntityDefinition entity,
        Dictionary<Guid, List<PropertyDefinition>> propertiesByEntityId)
    {
        if (!string.IsNullOrWhiteSpace(entity.PrimaryKey))
            return entity.PrimaryKey;

        if (propertiesByEntityId.TryGetValue(entity.Id, out var properties))
        {
            var pkProperty = properties.FirstOrDefault(p =>
                entity.PrimaryKey?.Equals(p.Name, StringComparison.OrdinalIgnoreCase) == true);
            if (pkProperty != null)
                return pkProperty.Name;
        }

        return "id"; // Default
    }

    /// <summary>Ensures the source table has a column for the foreign key; adds it if missing (e.g. Order has customer_id for Order→Customer).</summary>
    private static void EnsureForeignKeyColumn(
        Models.DatabaseSchema schema,
        string sourceTableName,
        string fkColumnName,
        string targetTableName,
        string targetPkColumnName)
    {
        var sourceTable = schema.Tables.FirstOrDefault(t => t.Name == sourceTableName);
        if (sourceTable == null) return;
        if (sourceTable.Columns.Any(c => c.Name.Equals(fkColumnName, StringComparison.OrdinalIgnoreCase)))
            return;

        var targetTable = schema.Tables.FirstOrDefault(t => t.Name == targetTableName);
        var targetPkColumn = targetTable?.Columns.FirstOrDefault(c =>
            c.Name.Equals(targetPkColumnName, StringComparison.OrdinalIgnoreCase));
        var fkSqlType = targetPkColumn?.SqlDataType ?? "uuid";

        var maxOrder = sourceTable.Columns.Count > 0 ? sourceTable.Columns.Max(c => c.Order) : 0;
        sourceTable.Columns.Add(new Models.ColumnSchema
        {
            Name = fkColumnName,
            DisplayName = fkColumnName,
            SqlDataType = fkSqlType,
            IsNullable = true,
            IsPrimaryKey = false,
            Order = maxOrder + 1
        });
    }
}
