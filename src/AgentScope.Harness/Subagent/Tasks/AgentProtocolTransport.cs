using AgentScope.Harness.Subagent.Protocol;

namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>Agent Protocol 传输实现，对�?Java AgentProtocolTransport</summary>
public sealed class AgentProtocolTransport : IRemoteSubagentTransport
{
    public const string TypeValue = "agent-protocol";
    private readonly AgentProtocolTaskClient _client;

    public AgentProtocolTransport(AgentProtocolTaskClient? client = null)
    {
        _client = client ?? new AgentProtocolTaskClient();
    }

    public string TransportType => TypeValue;

    public Task SubmitAsync(RemoteTarget target, string taskId,
        string agentId, string input, RemoteSubmitContext? context = null,
        CancellationToken ct = default)
        => _client.SubmitTaskAsync(target.BaseUrl, target.Headers,
            taskId, agentId, input, context, ct);

    public Task<RemoteTaskStatus> GetStatusAsync(RemoteTarget target,
        string taskId, CancellationToken ct = default)
        => _client.GetStatusAsync(target.BaseUrl, target.Headers, taskId, ct);

    public Task<string?> WaitForResultAsync(RemoteTarget target,
        string taskId, long timeoutSeconds, CancellationToken ct = default)
        => _client.WaitForResultAsync(target.BaseUrl, target.Headers,
            taskId, timeoutSeconds, ct);

    public Task CancelAsync(RemoteTarget target, string taskId,
        CancellationToken ct = default)
        => _client.CancelTaskAsync(target.BaseUrl, target.Headers, taskId, ct);

    public Task ResumeAsync(RemoteTarget target, string taskId,
        List<RemoteConfirmDecision> decisions, CancellationToken ct = default)
        => _client.ResumeTaskAsync(target.BaseUrl, target.Headers,
            taskId, decisions, ct);
}


