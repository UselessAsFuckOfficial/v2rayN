namespace ServiceLib.Models.Dto;

/// <summary>
/// One step of a workflow. Kept as a plain serializable DTO so it can be
/// persisted inside <see cref="Entities.WorkflowItem.StepsJson"/>. Implements
/// <see cref="INotifyPropertyChanged"/> so the step grid reacts to edits.
/// </summary>
[Serializable]
public class WorkflowStep : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

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
}
