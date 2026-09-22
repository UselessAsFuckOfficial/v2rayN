namespace ServiceLib.Models.Dto;

/// <summary>
/// One step of a workflow. Kept as a plain serializable DTO so it can be
/// persisted inside <see cref="Entities.WorkflowItem.StepsJson"/>.
/// </summary>
[Serializable]
public class WorkflowStep
{
    public EWorkflowAction Action { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>Subscription id for <see cref="EWorkflowAction.UpdateSubscriptions"/>; empty means all.</summary>
    public string? SubId { get; set; }

    /// <summary>Action dependent argument: sort column, test type or system proxy type.</summary>
    public string? Parameter { get; set; }

    /// <summary>Action dependent flag: ascending sort, or update through proxy.</summary>
    public bool BoolParameter { get; set; }
}
