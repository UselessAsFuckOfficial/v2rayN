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

    /// <summary>Editor-only action name, so the step grid can offer a plain text dropdown.</summary>
    [JsonIgnore]
    public string? ActionDisplay
    {
        get => Action.ToString();
        set
        {
            if (Enum.TryParse<EWorkflowAction>(value, true, out var action))
            {
                Action = action;
            }
        }
    }

    /// <summary>
    /// Editor-only group name. <see cref="ViewModels.WorkflowEditViewModel"/> resolves it
    /// back into <see cref="SubId"/> when the workflow is saved.
    /// </summary>
    [JsonIgnore]
    public string? SubDisplay { get; set; }

    /// <summary>Editor-only action choices, so the step grid can bind its dropdown to the row item.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> ActionOptions { get; set; } = [];

    /// <summary>
    /// Editor-only group choices. This is the editor's live collection, so subscriptions
    /// that finish loading after a row was created still reach that row's dropdown.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<string> GroupOptions { get; set; } = [];
}
