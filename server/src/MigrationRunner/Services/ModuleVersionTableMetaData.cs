using FluentMigrator.Runner.VersionTableInfo;

namespace MigrationRunner.Services;

/// <summary>
/// Per-module version table so each module (Tenant, Identity, User, Feature) tracks its own applied migrations.
/// Uses table name "{ModuleName}_VersionInfo" in the default (public) schema.
/// </summary>
internal sealed class ModuleVersionTableMetaData : IVersionTableMetaData
{
    private readonly string _moduleName;

    public ModuleVersionTableMetaData(string moduleName)
    {
        _moduleName = moduleName ?? throw new ArgumentNullException(nameof(moduleName));
    }

    public string SchemaName => string.Empty;
    public string TableName => $"{_moduleName}_VersionInfo";
    public string ColumnName => "Version";
    public string DescriptionColumnName => "Description";
    public string AppliedOnColumnName => "AppliedOn";
    public string UniqueIndexName => $"UC_{_moduleName}_Version";
    public bool OwnsSchema => false;
    public bool CreateWithPrimaryKey => true;
}
