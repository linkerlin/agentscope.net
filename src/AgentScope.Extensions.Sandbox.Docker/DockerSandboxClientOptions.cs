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

namespace AgentScope.Extensions.Sandbox.Docker;

/// <summary>
/// Docker 沙箱客户端创建选项。对标 Java DockerSandboxClientOptions。
/// </summary>
public sealed class DockerSandboxClientOptions
{
    /// <summary>Docker 镜像。默认 ubuntu:22.04。</summary>
    public string Image { get; set; } = "ubuntu:22.04";

    /// <summary>容器内工作区根路径。默认 /workspace。</summary>
    public string WorkspaceRoot { get; set; } = "/workspace";

    /// <summary>容器名（不指定则自动生成）。</summary>
    public string? ContainerName { get; set; }

    /// <summary>注入容器的环境变量。</summary>
    public Dictionary<string, string> Environment { get; set; } = new();

    /// <summary>内存上限（字节）。</summary>
    public long? MemorySizeBytes { get; set; }

    /// <summary>CPU 核数上限。</summary>
    public long? CpuCount { get; set; }

    /// <summary>暴露的宿主机端口。</summary>
    public int[] ExposedPorts { get; set; } = [];

    /// <summary>Docker 网络模式或网络名。</summary>
    public string? Network { get; set; }

    /// <summary>追加到 docker run 的额外原始参数（镜像名之前）。</summary>
    public List<string> AdditionalRunArgs { get; set; } = new();
}
