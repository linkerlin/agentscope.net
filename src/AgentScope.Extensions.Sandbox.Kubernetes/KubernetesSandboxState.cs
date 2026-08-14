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

namespace AgentScope.Extensions.Sandbox.Kubernetes;

/// <summary>
/// Kubernetes 沙箱的可序列化状态。对标 Java KubernetesSandboxState。
/// </summary>
public sealed record KubernetesSandboxState(
    string? SessionId = null,
    string? Namespace = null,
    string? ClaimName = null,
    string? SandboxName = null,
    string? WarmPoolName = null,
    string? PodName = null,
    string? PodIP = null,
    string WorkspaceRoot = "/workspace",
    string FileApiBaseDir = "/workspace",
    bool ClaimOwned = true)
{
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
                ["type"] = "kubernetes",
                ["state"] = JsonSerializer.Serialize(this),
            },
        };
    }

    /// <summary>从核心 <see cref="SandboxState"/> 的 ProviderData 恢复本记录。</summary>
    public static KubernetesSandboxState FromSandboxState(SandboxState state)
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
                var restored = JsonSerializer.Deserialize<KubernetesSandboxState>(json);
                if (restored != null) return restored;
            }
        }

        return new KubernetesSandboxState(
            SessionId: state.SessionId,
            WorkspaceRoot: state.WorkspaceRoot ?? "/workspace");
    }
}
