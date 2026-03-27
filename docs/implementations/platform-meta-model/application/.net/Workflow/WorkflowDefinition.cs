using System.Text.Json.Serialization;

namespace PlatformMetaModel.Workflow;

/// <summary>
/// Workflow definition; BPMN-aligned: startEvent, tasks, sequenceFlows.
/// </summary>
public class WorkflowDefinition
{
    public required string Id { get; set; }

    /// <summary>Entity id this workflow is bound to.</summary>
    public required string Entity { get; set; }

    public string? TenantId { get; set; }

    public required WorkflowStartEvent StartEvent { get; set; }

    /// <summary>Workflow tasks; each has id, type, optional requiredFields and config.</summary>
    public required IList<WorkflowTaskDefinition> Tasks { get; set; }

    /// <summary>Transitions between tasks; condition evaluated before allowing transition.</summary>
    public IList<WorkflowSequenceFlowDefinition>? SequenceFlows { get; set; }
}

/// <summary>Start event for workflow.</summary>
public class WorkflowStartEvent
{
    [JsonPropertyName("type")]
    public required WorkflowStartEventType Type { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkflowStartEventType
{
    OnCreate,
    OnUpdate,
    OnDelete,
    Manual,
    Timer
}

/// <summary>
/// Single task in a workflow. Backend enforces requiredFields when completing this task or when transitioning out.
/// </summary>
public class WorkflowTaskDefinition
{
    /// <summary>Task identifier; referenced in sequenceFlows from/to.</summary>
    public required string Id { get; set; }

    [JsonPropertyName("type")]
    public required WorkflowTaskType Type { get; set; }

    /// <summary>Entity property names that must be set before this task can complete or before any outgoing transition.</summary>
    public IList<string>? RequiredFields { get; set; }

    /// <summary>Task-type-specific parameters.</summary>
    public Dictionary<string, object>? Config { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WorkflowTaskType
{
    UserTask,
    UpdateField,
    CreateEntity,
    SendNotification,
    CustomAction
}

/// <summary>
/// Flow between two tasks. Condition is evaluated by backend before allowing transition.
/// </summary>
public class WorkflowSequenceFlowDefinition
{
    /// <summary>Source task id.</summary>
    public required string From { get; set; }

    /// <summary>Target task id.</summary>
    public required string To { get; set; }

    /// <summary>Optional expression; when false, transition is refused.</summary>
    public string? Condition { get; set; }
}
