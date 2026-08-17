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

using System.Text.Json;

namespace AgentScope.Extensions.Sandbox.AgentRun;

/// <summary>
/// Serializable state for an AgentRun sandbox.
/// Counterpart of Java AgentRunSandboxState.
/// <br/>
/// AgentRun 沙箱的可序列化状态。对标 Java AgentRunSandboxState。
/// </summary>
/// <param name="SessionId">Session identifier / 会话标识符</param>
/// <param name="SandboxId">Sandbox identifier / 沙箱标识符</param>
/// <param name="WorkspaceRoot">Workspace root path inside container / 容器内工作区根路径</param>
/// <param name="TemplateName">AgentRun template name / AgentRun 模板名称</param>
/// <param name="AccountId">Alibaba Cloud account ID / 阿里云账号 ID</param>
/// <param name="Region">Alibaba Cloud region / 阿里云地域</param>
/// <param name="McpServerUrl">MCP server URL / MCP 服务端地址</param>
/// <param name="SandboxOwned">Whether this instance owns the sandbox / 此实例是否拥有沙箱所有权</param>
/// <param name="WorkspaceOnNas">Whether workspace is on NAS / 工作区是否在 NAS 上</param>
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
    /// <summary>
    /// Default workspace root path inside the AgentRun sandbox container.
    /// AgentRun 沙箱容器内默认工作区根路径。
    /// </summary>
    public const string DefaultWorkspaceRoot = "/home/agentscope/workspace";

    /// <summary>
    /// Converts this state to a core <see cref="SandboxState"/>, serializing this record into ProviderData.
    /// 构建核心 <see cref="SandboxState"/>，并将本记录序列化进 ProviderData。
    /// </summary>
    /// <param name="spec">Workspace specification / 工作区规格</param>
    /// <param name="snapshotRef">Optional snapshot reference / 可选的快照引用</param>
    /// <returns>The core SandboxState / 核心 SandboxState</returns>
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

    /// <summary>
    /// Restores this state from the ProviderData of a core <see cref="SandboxState"/>.
    /// 从核心 <see cref="SandboxState"/> 的 ProviderData 恢复本记录。
    /// </summary>
    /// <param name="state">Core sandbox state / 核心沙箱状态</param>
    /// <returns>Restored AgentRunSandboxState / 恢复后的 AgentRunSandboxState</returns>
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
