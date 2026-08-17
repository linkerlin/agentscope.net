// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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


