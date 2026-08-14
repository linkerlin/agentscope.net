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

namespace AgentScope.Extensions.Sandbox.Daytona;

/// <summary>
/// Daytona 沙箱客户端创建选项。对标 Java DaytonaSandboxClientOptions。
/// </summary>
public sealed class DaytonaSandboxClientOptions
{
    /// <summary>Daytona API Key。</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>控制面基础地址。</summary>
    public string ControlPlaneBaseUrl { get; set; } = "https://app.daytona.io/api";

    /// <summary>工具箱代理基础地址。</summary>
    public string ToolboxBaseUrl { get; set; } = "https://proxy.app.daytona.io";

    /// <summary>沙箱镜像。</summary>
    public string Image { get; set; } = "ubuntu:22.04";

    /// <summary>快照 id（从快照创建时使用）。</summary>
    public string? SnapshotId { get; set; }

    /// <summary>CPU 核数。</summary>
    public int Cpu { get; set; } = 1;

    /// <summary>内存（GiB）。</summary>
    public int Memory { get; set; } = 1;

    /// <summary>磁盘（GiB）。</summary>
    public int Disk { get; set; } = 3;

    /// <summary>容器内工作区根路径。</summary>
    public string WorkspaceRoot { get; set; } = DaytonaSandboxState.DefaultWorkspaceRoot;

    /// <summary>连接超时（秒）。</summary>
    public int ConnectTimeoutSeconds { get; set; } = 30;

    /// <summary>读取超时（秒）。</summary>
    public int ReadTimeoutSeconds { get; set; } = 120;

    /// <summary>最大重试次数。</summary>
    public int MaxRetries { get; set; } = 3;
}
