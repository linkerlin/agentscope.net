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

namespace AgentScope.Harness.Filesystem.Spec;

/// <summary>
/// 远程文件系统规格：描述远程文件系统的端点、命名空间、凭据引用。
/// 对应 Java: io.agentscope.harness.agent.filesystem.spec.RemoteFilesystemSpec
/// </summary>
public sealed record RemoteFilesystemSpec(
    string Endpoint,
    string Namespace,
    string? CredentialRef = null,
    bool Tls = true)
{
    /// <summary>是否启用 TLS。</summary>
    public string Scheme => Tls ? "https" : "http";
}
