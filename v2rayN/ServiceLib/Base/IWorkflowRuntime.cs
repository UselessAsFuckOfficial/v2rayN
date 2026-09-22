namespace ServiceLib.Base;

/// <summary>
/// The operations a workflow can invoke at runtime. Implemented by the UI layer
/// so that <see cref="Handler.WorkflowHandler"/> stays free of view models.
/// </summary>
public interface IWorkflowRuntime
{
    /// <summary>Update one subscription (or all when <paramref name="subId"/> is empty).</summary>
    Task UpdateSubscriptions(string? subId, bool viaProxy);

    /// <summary>Remove duplicate servers in a group. Returns the number of removed servers.</summary>
    Task<int> DeduplicateServers(string subId);

    /// <summary>Sort a group by a column.</summary>
    Task SortServers(string subId, string column, bool asc);

    /// <summary>Run a connectivity/speed test over a group.</summary>
    Task TestServers(string subId, ESpeedActionType actionType);

    /// <summary>Remove servers whose last test result was invalid. Returns the number of removed servers.</summary>
    Task<int> RemoveInvalidServers(string subId);

    /// <summary>Select the first server of a group as the default one.</summary>
    Task SetDefaultServer(string subId);

    /// <summary>Set the system proxy mode.</summary>
    Task SetSystemProxy(ESysProxyType type);
}
