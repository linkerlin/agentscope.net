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

namespace AgentScope.Harness.Sandbox;

/// <summary>
/// Docker 沙箱文件系统规格：描述容器内工作目录、绑定挂载与用户映射。
/// 对应 Java: io.agentscope.harness.agent.sandbox.impl.docker.DockerFilesystemSpec
/// </summary>
public sealed record DockerFilesystemSpec(
    string ContainerWorkspace = "/workspace",
    string? HostMountSource = null,
    bool ReadOnly = false,
    string? UserId = null)
{
    /// <summary>宿主到容器的绑定挂载目标。</summary>
    public string MountTarget => ContainerWorkspace;
}
