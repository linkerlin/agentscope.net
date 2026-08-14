namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>任务运行规格，对�?Java TaskRunSpec</summary>
public abstract record TaskRunSpec;

/// <summary>本地执行任务</summary>
public sealed record LocalTaskRunSpec(Func<Task<string?>> Execution) : TaskRunSpec;

/// <summary>远程 HTTP 任务</summary>
public sealed record RemoteTaskRunSpec(
    string BaseUrl,
    Dictionary<string, string>? Headers,
    string AgentId,
    string Input,
    RemoteSubmitContext? Context = null) : TaskRunSpec;

/// <summary>适配已有 Task 的任�?/summary>
public sealed record AdoptedTaskRunSpec(Task<string?> Future) : TaskRunSpec;

