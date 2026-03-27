namespace PlatformMetaModel.Common;

/// <summary>
/// Audit and soft-delete fields: who created/updated (or deleted) and when.
/// </summary>
public class AuditDefinition
{
    /// <summary>When the record was created.</summary>
    public required string CreatedAt { get; set; }

    /// <summary>Who created the record (user id, actor id).</summary>
    public required string CreatedBy { get; set; }

    /// <summary>When the record was last updated.</summary>
    public string? UpdatedAt { get; set; }

    /// <summary>Who last updated the record.</summary>
    public string? UpdatedBy { get; set; }

    /// <summary>When the record was soft-deleted; absent or null means not deleted.</summary>
    public string? DeletedAt { get; set; }

    /// <summary>Who soft-deleted the record.</summary>
    public string? DeletedBy { get; set; }
}
