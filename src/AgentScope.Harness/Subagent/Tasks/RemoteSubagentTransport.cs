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

