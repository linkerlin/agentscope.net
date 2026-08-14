using AgentScope.Harness.Subagent.Protocol;
namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>远程子代理传输层接口，对�?Java RemoteSubagentTransport</summary>
public interface IRemoteSubagentTransport
{
    string TransportType { get; }

    Task SubmitAsync(RemoteTarget target, string taskId,
        string agentId, string input, RemoteSubmitContext? context = null,
        CancellationToken ct = default);

    Task<RemoteTaskStatus> GetStatusAsync(RemoteTarget target,
        string taskId, CancellationToken ct = default);

    Task<string?> WaitForResultAsync(RemoteTarget target,
        string taskId, long timeoutSeconds,
        CancellationToken ct = default);

    Task CancelAsync(RemoteTarget target, string taskId,
        CancellationToken ct = default);

    Task ResumeAsync(RemoteTarget target, string taskId,
        List<RemoteConfirmDecision> decisions,
        CancellationToken ct = default);
}

