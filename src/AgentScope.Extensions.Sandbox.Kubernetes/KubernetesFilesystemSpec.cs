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
/// Kubernetes 沙箱的文件系统规格：描述容器内工作目录、镜像与命名空间。对标 Java KubernetesFilesystemSpec。
/// </summary>
public sealed record KubernetesFilesystemSpec(
    string ContainerWorkspace = "/workspace",
    string Image = "ubuntu:22.04",
    string Namespace = "default",
    string? WarmPoolName = null,
    string FileApiBaseDir = "/workspace")
{
    /// <summary>容器内工作区根路径。</summary>
    public string WorkspaceRoot => ContainerWorkspace;
}
