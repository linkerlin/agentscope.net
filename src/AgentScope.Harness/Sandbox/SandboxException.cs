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
/// 沙箱异常，携带错误码与可选的沙箱ID。
/// Sandbox exception carrying an error code and an optional sandbox ID.
/// 对应 Java: io.agentscope.harness.agent.sandbox.SandboxException
/// </summary>
public class SandboxException : System.Exception
{
    /// <summary>
    /// 沙箱错误码。
    /// Sandbox error code.
    /// </summary>
    public SandboxErrorCode ErrorCode { get; }

    /// <summary>
    /// 关联的沙箱ID（可选）。
    /// Associated sandbox ID (optional).
    /// </summary>
    public string? SandboxId { get; }

    /// <summary>
    /// 以错误信息和错误码创建异常。
    /// Create an exception with an error message and error code.
    /// </summary>
    /// <param name="message">错误描述 / Error description</param>
    /// <param name="errorCode">错误码 / Error code</param>
    /// <param name="sandboxId">沙箱ID / Sandbox ID</param>
    public SandboxException(string message, SandboxErrorCode errorCode = SandboxErrorCode.Unknown, string? sandboxId = null)
        : base(message)
    {
        ErrorCode = errorCode;
        SandboxId = sandboxId;
    }

    /// <summary>
    /// 以错误信息、内部异常和错误码创建异常。
    /// Create an exception with an error message, inner exception, and error code.
    /// </summary>
    /// <param name="message">错误描述 / Error description</param>
    /// <param name="inner">内部异常 / Inner exception</param>
    /// <param name="errorCode">错误码 / Error code</param>
    /// <param name="sandboxId">沙箱ID / Sandbox ID</param>
    public SandboxException(string message, System.Exception inner, SandboxErrorCode errorCode = SandboxErrorCode.Unknown, string? sandboxId = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
        SandboxId = sandboxId;
    }
}
