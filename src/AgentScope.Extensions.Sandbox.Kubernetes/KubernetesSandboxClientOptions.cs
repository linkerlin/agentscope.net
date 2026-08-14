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

namespace AgentScope.Extensions.Sandbox.Kubernetes;

/// <summary>
/// Kubernetes 沙箱客户端创建选项。对标 Java KubernetesSandboxClientOptions。
/// </summary>
public sealed class KubernetesSandboxClientOptions
{
    /// <summary>沙箱镜像（C# 侧通过 kubectl 创建 Pod 使用）。</summary>
    public string Image { get; set; } = "ubuntu:22.04";

    /// <summary>kubeconfig 路径（默认取 ~/.kube/config）。</summary>
    public string? KubeConfigPath { get; set; }

    /// <summary>命名空间。</summary>
    public string Namespace { get; set; } = "default";

    /// <summary>预热池名称（agent-sandbox 模式）。</summary>
    public string? WarmPoolName { get; set; }

    /// <summary>容器内工作区根路径。</summary>
    public string WorkspaceRoot { get; set; } = "/workspace";

    /// <summary>运行时文件 API 基础目录（/upload、/download）。</summary>
    public string FileApiBaseDir { get; set; } = "/workspace";

    /// <summary>直连 API 地址（agent-sandbox 连接策略）。</summary>
    public string? ApiUrl { get; set; }

    /// <summary>网关名称。</summary>
    public string? GatewayName { get; set; }

    /// <summary>网关命名空间。</summary>
    public string? GatewayNamespace { get; set; }

    /// <summary>网关协议。</summary>
    public string GatewayScheme { get; set; } = "http";

    /// <summary>服务端口。</summary>
    public int ServerPort { get; set; } = 8888;

    /// <summary>沙箱就绪超时（秒）。</summary>
    public long SandboxReadyTimeoutSeconds { get; set; } = 180;

    /// <summary>清理超时（秒）。</summary>
    public long CleanupTimeoutSeconds { get; set; } = 30;

    /// <summary>请求超时（秒）。</summary>
    public long RequestTimeoutSeconds { get; set; } = 180;

    /// <summary>单次尝试超时（秒）。</summary>
    public long PerAttemptTimeoutSeconds { get; set; } = 60;

    /// <summary>端口转发超时（秒）。</summary>
    public long PortForwardTimeoutSeconds { get; set; } = 30;
}
