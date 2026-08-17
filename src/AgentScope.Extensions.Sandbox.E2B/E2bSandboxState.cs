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

namespace AgentScope.Extensions.Sandbox.E2B;

/// <summary>
/// E2B 沙箱的可序列化状态。对标 Java E2bSandboxState。
/// </summary>
public sealed record E2bSandboxState(
    string? SessionId = null,
    string? SandboxId = null,
    string TemplateId = "base",
    string? SandboxDomain = null,
    string? EnvdAccessToken = null,
    string EnvdVersion = "0.1.5",
    string WorkspaceRoot = "/home/user",
    bool SandboxOwned = true,
    E2bPersistenceMode PersistenceMode = E2bPersistenceMode.Tar)
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
                ["type"] = "e2b",
                ["state"] = JsonSerializer.Serialize(this),
            },
        };
    }

    /// <summary>从核心 <see cref="SandboxState"/> 的 ProviderData 恢复本记录。</summary>
    public static E2bSandboxState FromSandboxState(SandboxState state)
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
                var restored = JsonSerializer.Deserialize<E2bSandboxState>(json);
                if (restored != null) return restored;
            }
        }

        return new E2bSandboxState(
            SessionId: state.SessionId,
            WorkspaceRoot: state.WorkspaceRoot ?? "/home/user");
    }
}
