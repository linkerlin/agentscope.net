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

namespace AgentScope.Extensions.Sandbox.E2B;

/// <summary>
/// E2B 沙箱客户端创建选项。对标 Java E2bSandboxClientOptions。
/// </summary>
public sealed class E2bSandboxClientOptions
{
    /// <summary>E2B API Key（必需）。</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>E2B API 基础地址。</summary>
    public string ApiBaseUrl { get; set; } = "https://api.e2b.dev/v1";

    /// <summary>E2B 域名。</summary>
    public string Domain { get; set; } = "e2b.app";

    /// <summary>E2B 模板 id（或从快照创建时的 snapshot id）。</summary>
    public string TemplateId { get; set; } = "base";

    /// <summary>沙箱内工作区根的绝对路径。</summary>
    public string WorkspaceRoot { get; set; } = "/home/user";

    /// <summary>沙箱空闲超时（秒）。</summary>
    public int SandboxTimeoutSeconds { get; set; } = 300;

    /// <summary>沙箱内运行用户。</summary>
    public string RunUser { get; set; } = "user";

    /// <summary>工作区持久化方式。</summary>
    public E2bPersistenceMode PersistenceMode { get; set; } = E2bPersistenceMode.Tar;

    /// <summary>连接超时（秒）。</summary>
    public int ConnectTimeoutSeconds { get; set; } = 30;

    /// <summary>读取超时（秒）。</summary>
    public int ReadTimeoutSeconds { get; set; } = 120;

    /// <summary>最大重试次数。</summary>
    public int MaxRetries { get; set; } = 3;
}
