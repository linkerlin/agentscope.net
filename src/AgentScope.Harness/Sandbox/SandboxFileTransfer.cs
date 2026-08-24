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

namespace AgentScope.Harness.Sandbox;

/// <summary>
/// 沙箱文件传输：在宿主与沙箱之间上传/下载文件与目录。
/// Sandbox file transfer: upload/download files and directories between host and sandbox.
/// 对应 Java: io.agentscope.harness.agent.sandbox.SandboxFileTransfer
/// </summary>
public interface ISandboxFileTransfer
{
    /// <summary>
    /// 上传单个文件到沙箱。
    /// Upload a single file to the sandbox.
    /// </summary>
    Task UploadAsync(string localPath, string remotePath, CancellationToken ct = default);

    /// <summary>
    /// 从沙箱下载文件到宿主。
    /// Download a file from the sandbox to the host.
    /// </summary>
    Task DownloadAsync(string remotePath, string localPath, CancellationToken ct = default);

    /// <summary>
    /// 上传目录（递归）。
    /// Upload a directory (recursive).
    /// </summary>
    Task UploadDirectoryAsync(string localDir, string remoteDir, CancellationToken ct = default);

    /// <summary>
    /// 下载目录（递归）。
    /// Download a directory (recursive).
    /// </summary>
    Task DownloadDirectoryAsync(string remoteDir, string localDir, CancellationToken ct = default);
}

/// <summary>
/// 沙箱感知能力标记：运行期组件可声明自己需要/绑定到某个沙箱上下文。
/// Sandbox-aware marker: runtime components can declare binding to a sandbox context.
/// 对应 Java: io.agentscope.harness.agent.sandbox.SandboxAware
/// </summary>
public interface ISandboxAware
{
    /// <summary>
    /// 绑定的沙箱ID（未绑定时为 null）。
    /// Bound sandbox ID (null when not bound).
    /// </summary>
    string? SandboxId { get; }

    /// <summary>
    /// 绑定沙箱上下文。
    /// Bind to a sandbox context.
    /// </summary>
    void BindSandbox(string sandboxId);
}
