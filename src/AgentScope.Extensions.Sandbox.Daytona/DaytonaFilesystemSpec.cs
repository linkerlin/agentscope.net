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

namespace AgentScope.Extensions.Sandbox.Daytona;

/// <summary>
/// Daytona sandbox filesystem specification.
/// Describes the container working directory, image, and resource quotas.
/// Counterpart of Java DaytonaFilesystemSpec.
/// <br/>
/// Daytona 沙箱的文件系统规格。
/// 描述容器内工作目录、镜像与资源配额。对标 Java DaytonaFilesystemSpec。
/// </summary>
/// <param name="ContainerWorkspace">Container workspace root path / 容器内工作区根路径</param>
/// <param name="Image">Container image / 容器镜像</param>
/// <param name="SnapshotId">Snapshot ID for creating from snapshot / 从快照创建时的快照 ID</param>
/// <param name="Cpu">Number of CPU cores / CPU 核数</param>
/// <param name="Memory">Memory in GiB / 内存（GiB）</param>
/// <param name="Disk">Disk size in GiB / 磁盘（GiB）</param>
public sealed record DaytonaFilesystemSpec(
    string ContainerWorkspace = DaytonaSandboxState.DefaultWorkspaceRoot,
    string Image = "ubuntu:22.04",
    string? SnapshotId = null,
    int Cpu = 1,
    int Memory = 1,
    int Disk = 3)
{
    /// <summary>
    /// The workspace root path inside the container.
    /// 容器内工作区根路径。
    /// </summary>
    public string WorkspaceRoot => ContainerWorkspace;
}
