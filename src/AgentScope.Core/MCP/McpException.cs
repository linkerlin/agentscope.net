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

using AgentScope.Core.Exception;

namespace AgentScope.Core.MCP;

/// <summary>
/// Exception representing MCP client or protocol errors.
/// Corresponds to Java: io.agentscope.core.mcp.McpException
/// MCP 客户端或协议错误。
/// 对应 Java: io.agentscope.core.mcp.McpException
/// </summary>
public class McpException : AgentScopeException
{
    /// <summary>
    /// Initializes a new instance of <see cref="McpException"/> with a message.
    /// 使用消息初始化 McpException 的新实例。
    /// </summary>
    /// <param name="message">The error message / 错误消息</param>
    public McpException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="McpException"/> with a message and inner exception.
    /// 使用消息和内部异常初始化 McpException 的新实例。
    /// </summary>
    /// <param name="message">The error message / 错误消息</param>
    /// <param name="innerException">The inner exception / 内部异常</param>
    public McpException(string message, global::System.Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Gets or sets the optional error code.
    /// 获取或设置可选的错误代码。
    /// </summary>
    public int? Code { get; init; }
}