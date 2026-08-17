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
/// E2B 云沙箱的文件系统规格：描述容器内工作目录与模板/快照。对标 Java E2bFilesystemSpec。
/// </summary>
public sealed record E2bFilesystemSpec(
    string ContainerWorkspace = "/home/user",
    string TemplateId = "base",
    string? SnapshotId = null,
    string Domain = "e2b.app")
{
    /// <summary>容器内工作区根路径。</summary>
    public string WorkspaceRoot => ContainerWorkspace;
}
