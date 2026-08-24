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

namespace AgentScope.Core.State;

/// <summary>
/// Exception thrown when a session state is concurrently modified (CAS failure).
/// 会话状态被并发修改时抛出的异常（CAS 失败）。
/// Corresponds to Java: io.agentscope.core.state.ConcurrentSessionModificationException
/// 对应 Java: io.agentscope.core.state.ConcurrentSessionModificationException
/// </summary>
public class ConcurrentSessionModificationException : System.Exception
{
    /// <summary>
    /// Initializes a new instance with the specified error message.
    /// 使用指定的错误消息初始化新实例。
    /// </summary>
    /// <param name="message">Error message describing the conflict / 描述冲突的错误消息</param>
    public ConcurrentSessionModificationException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with the specified error message and inner exception.
    /// 使用指定的错误消息和内部异常初始化新实例。
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    /// <param name="innerException">Inner exception that caused this error / 导致此错误的内部异常</param>
    public ConcurrentSessionModificationException(string message, System.Exception innerException)
        : base(message, innerException) { }
}
