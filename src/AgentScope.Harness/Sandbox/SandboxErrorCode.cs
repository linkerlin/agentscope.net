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
/// 沙箱错误码。对应 Java: io.agentscope.harness.agent.sandbox.SandboxErrorCode
/// Sandbox error codes. Counterpart to Java SandboxErrorCode.
/// </summary>
public enum SandboxErrorCode
{
    /// <summary>
    /// 未知错误。
    /// Unknown error.
    /// </summary>
    Unknown,

    /// <summary>
    /// 获取沙箱失败。
    /// Failed to acquire sandbox.
    /// </summary>
    AcquireFailed,

    /// <summary>
    /// 命令执行失败。
    /// Command execution failed.
    /// </summary>
    ExecFailed,

    /// <summary>
    /// 操作超时。
    /// Operation timed out.
    /// </summary>
    Timeout,

    /// <summary>
    /// 沙箱未找到。
    /// Sandbox not found.
    /// </summary>
    NotFound,

    /// <summary>
    /// 沙箱尚未就绪。
    /// Sandbox not ready.
    /// </summary>
    NotReady,

    /// <summary>
    /// 快照创建失败。
    /// Snapshot creation failed.
    /// </summary>
    SnapshotFailed,

    /// <summary>
    /// 快照恢复失败。
    /// Snapshot restoration failed.
    /// </summary>
    RestoreFailed,

    /// <summary>
    /// 文件传输失败。
    /// File transfer failed.
    /// </summary>
    FileTransferFailed,

    /// <summary>
    /// 配额超出限制。
    /// Quota exceeded.
    /// </summary>
    QuotaExceeded,

    /// <summary>
    /// 未授权操作。
    /// Unauthorized operation.
    /// </summary>
    Unauthorized
}
