namespace ServiceLib.Tests.Handler;

public class WorkflowHandlerTests
{
    [Test]
    public async Task SaveSteps_ThenDeserialize_PreservesStepFields()
    {
        var workflow = new WorkflowItem { Id = "w1", Remarks = "nightly" };
        List<WorkflowStep> steps =
        [
            new() { Action = EWorkflowAction.UpdateSubscriptions, SubId = "sub-1", BoolParameter = true },
            new() { Action = EWorkflowAction.SortServers, Parameter = nameof(EServerColName.SpeedVal), BoolParameter = false },
            new() { Action = EWorkflowAction.SystemProxy, Parameter = nameof(ESysProxyType.ForcedClear), Enabled = false },
        ];

        WorkflowHandler.SaveSteps(workflow, steps);
        var loaded = WorkflowHandler.DeserializeSteps(workflow);

        await loaded.Count.Should().BeEqualTo(3);
        await loaded[0].Action.Should().BeEqualTo(EWorkflowAction.UpdateSubscriptions);
        await loaded[0].SubId.Should().BeEqualTo("sub-1");
        await loaded[0].BoolParameter.Should().BeTrue();
        await loaded[1].Parameter.Should().BeEqualTo(nameof(EServerColName.SpeedVal));
        await loaded[2].Enabled.Should().BeFalse();
    }

    [Test]
    public async Task SaveSteps_ShouldNotPersistEditorOnlyFields()
    {
        var workflow = new WorkflowItem { Id = "w1", Remarks = "nightly" };
        List<WorkflowStep> steps =
        [
            new()
            {
                Action = EWorkflowAction.SortServers,
                SubDisplay = "Group A",
                Parameter = "DelayVal",
                ActionOptions = ["SortServers"],
                GroupOptions = ["Group A"],
            },
        ];

        WorkflowHandler.SaveSteps(workflow, steps);

        await workflow.StepsJson.Contains("Group A").Should().BeFalse();
        await workflow.StepsJson.Contains("SubDisplay").Should().BeFalse();
        await workflow.StepsJson.Contains("ActionDisplay").Should().BeFalse();
        await workflow.StepsJson.Contains("ActionOptions").Should().BeFalse();
        await workflow.StepsJson.Contains("GroupOptions").Should().BeFalse();
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("not json")]
    [Arguments("{\"not\":\"a list\"}")]
    public async Task DeserializeSteps_ShouldTolerateMissingOrMalformedJson(string? json)
    {
        var workflow = new WorkflowItem { Id = "w1", Remarks = "nightly", StepsJson = json };

        var loaded = WorkflowHandler.DeserializeSteps(workflow);

        await loaded.Count.Should().BeEqualTo(0);
    }

    [Test]
    public async Task DeserializeSteps_ShouldHandleNullWorkflow()
    {
        await WorkflowHandler.DeserializeSteps(null).Count.Should().BeEqualTo(0);
    }

    [Test]
    public async Task Run_ShouldSkipDisabledStepsAndDispatchEveryAction()
    {
        var runtime = new RecordingRuntime();
        var workflow = new WorkflowItem { Id = "w1", Remarks = "nightly" };
        WorkflowHandler.SaveSteps(workflow,
        [
            new() { Action = EWorkflowAction.UpdateSubscriptions, BoolParameter = true },
            new() { Action = EWorkflowAction.DeduplicateServers },
            new() { Action = EWorkflowAction.SortServers, Parameter = nameof(EServerColName.SpeedVal), BoolParameter = false },
            new() { Action = EWorkflowAction.TestServers, Parameter = nameof(ESpeedActionType.Speedtest) },
            new() { Action = EWorkflowAction.RemoveInvalidServers, Enabled = false },
            new() { Action = EWorkflowAction.SetDefaultServer },
            new() { Action = EWorkflowAction.SystemProxy, Parameter = nameof(ESysProxyType.ForcedChange) },
        ]);

        await WorkflowHandler.Run(new Config { SubIndexId = "default-sub" }, workflow, runtime);

        await runtime.Calls.Should().BeEquivalentTo(
        [
            // A subscription step with no group updates every subscription, so SubId stays null.
            "update::True",
            "dedup:default-sub",
            "sort:default-sub:SpeedVal:False",
            "test:default-sub:Speedtest",
            "default:default-sub",
            "sysproxy:ForcedChange",
        ]);
    }

    [Test]
    public async Task Run_ShouldPreferStepSubIdOverConfigGroup()
    {
        var runtime = new RecordingRuntime();
        var workflow = new WorkflowItem { Id = "w1", Remarks = "nightly" };
        WorkflowHandler.SaveSteps(workflow,
        [
            new() { Action = EWorkflowAction.DeduplicateServers, SubId = "sub-9" },
        ]);

        await WorkflowHandler.Run(new Config { SubIndexId = "default-sub" }, workflow, runtime);

        await runtime.Calls.Should().BeEquivalentTo(["dedup:sub-9"]);
    }

    [Test]
    public async Task Run_ShouldFallBackToDefaultsForUnknownParameters()
    {
        var runtime = new RecordingRuntime();
        var workflow = new WorkflowItem { Id = "w1", Remarks = "nightly" };
        WorkflowHandler.SaveSteps(workflow,
        [
            new() { Action = EWorkflowAction.SortServers, Parameter = null },
            new() { Action = EWorkflowAction.TestServers, Parameter = "not-a-test-type" },
            new() { Action = EWorkflowAction.SystemProxy, Parameter = null },
        ]);

        await WorkflowHandler.Run(new Config { SubIndexId = "default-sub" }, workflow, runtime);

        await runtime.Calls.Should().BeEquivalentTo(
        [
            $"sort:default-sub:{nameof(EServerColName.DelayVal)}:False",
            $"test:default-sub:{nameof(ESpeedActionType.Realping)}",
            $"sysproxy:{nameof(ESysProxyType.ForcedClear)}",
        ]);
    }

    [Test]
    public async Task Run_ShouldNotDispatchAnythingWhenEveryStepIsDisabled()
    {
        var runtime = new RecordingRuntime();
        var workflow = new WorkflowItem { Id = "w1", Remarks = "nightly" };
        WorkflowHandler.SaveSteps(workflow,
        [
            new() { Action = EWorkflowAction.UpdateSubscriptions, Enabled = false },
        ]);

        await WorkflowHandler.Run(new Config { SubIndexId = "default-sub" }, workflow, runtime);

        await runtime.Calls.Count.Should().BeEqualTo(0);
    }

    /// <summary>
    /// Records the calls the handler makes so the dispatch logic can be asserted without
    /// touching the UI layer. This is an implementation of the runtime seam, not a mock.
    /// </summary>
    private sealed class RecordingRuntime : IWorkflowRuntime
    {
        public List<string> Calls { get; } = [];

        public Task UpdateSubscriptions(string? subId, bool viaProxy)
        {
            Calls.Add($"update:{subId}:{viaProxy}");
            return Task.CompletedTask;
        }

        public Task<int> DeduplicateServers(string subId)
        {
            Calls.Add($"dedup:{subId}");
            return Task.FromResult(0);
        }

        public Task SortServers(string subId, string column, bool asc)
        {
            Calls.Add($"sort:{subId}:{column}:{asc}");
            return Task.CompletedTask;
        }

        public Task TestServers(string subId, ESpeedActionType actionType)
        {
            Calls.Add($"test:{subId}:{actionType}");
            return Task.CompletedTask;
        }

        public Task<int> RemoveInvalidServers(string subId)
        {
            Calls.Add($"remove-invalid:{subId}");
            return Task.FromResult(0);
        }

        public Task SetDefaultServer(string subId)
        {
            Calls.Add($"default:{subId}");
            return Task.CompletedTask;
        }

        public Task SetSystemProxy(ESysProxyType type)
        {
            Calls.Add($"sysproxy:{type}");
            return Task.CompletedTask;
        }
    }
}
