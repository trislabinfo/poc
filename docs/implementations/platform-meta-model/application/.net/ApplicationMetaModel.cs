using PlatformMetaModel.Application;
using PlatformMetaModel.Persistence;

namespace PlatformMetaModel;

/// <summary>
/// Platform meta model (platform definition). Root object for application-meta-model.schema.json.
/// </summary>
public class ApplicationMetaModel
{
    /// <summary>Application definition (root container).</summary>
    public required ApplicationDefinition Application { get; set; }

    /// <summary>Persistence/database configuration.</summary>
    public required PersistenceDefinition Persistence { get; set; }
}
