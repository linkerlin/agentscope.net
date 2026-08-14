// Copyright 2024-2026 the original author or authors.
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

using System.Text.Json;

namespace AgentScope.Extensions.Sandbox.AgentRun;

/// <summary>
/// AgentRun 沙箱的可序列化状态。对标 Java AgentRunSandboxState。
/// </summary>
public sealed record AgentRunSandboxState(
    string? SessionId = null,
    string? SandboxId = null,
    string WorkspaceRoot = "/home/agentscope/workspace",
    string? TemplateName = null,
    string? AccountId = null,
    string? Region = null,
    string? McpServerUrl = null,
    bool SandboxOwned = true,
    bool WorkspaceOnNas = false)
{
    /// <summary>AgentRun 沙箱容器内默认工作区根路径。</summary>
    public const string DefaultWorkspaceRoot = "/home/agentscope/workspace";

    /// <summary>构建核心 <see cref="SandboxState"/>，并将本记录序列化进 ProviderData。</summary>
    public SandboxState ToSandboxState(WorkspaceSpec spec, string snapshotRef = "")
    {
        var sessionId = SessionId ?? Guid.NewGuid().ToString();
        return new SandboxState(sessionId, spec, snapshotRef)
        {
            SessionId = sessionId,
            WorkspaceRoot = WorkspaceRoot,
            ProviderData = new Dictionary<string, object?>
            {
                ["type"] = "agentrun",
                ["state"] = JsonSerializer.Serialize(this),
            },
        };
    }

    /// <summary>从核心 <see cref="SandboxState"/> 的 ProviderData 恢复本记录。</summary>
    public static AgentRunSandboxState FromSandboxState(SandboxState state)
    {
        if (state.ProviderData != null && state.ProviderData.TryGetValue("state", out var raw))
        {
            var json = raw switch
            {
                string s => s,
                JsonElement e => e.ValueKind == JsonValueKind.String ? e.GetString() : null,
                _ => null,
            };
            if (!string.IsNullOrEmpty(json))
            {
                var restored = JsonSerializer.Deserialize<AgentRunSandboxState>(json);
                if (restored != null) return restored;
            }
        }

        return new AgentRunSandboxState(
            SessionId: state.SessionId,
            WorkspaceRoot: state.WorkspaceRoot ?? DefaultWorkspaceRoot);
    }
}
