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

using System;

namespace AgentScope.Core.Exception;

/// <summary>
/// Base exception for AgentScope framework.
/// AgentScope 基础异常
/// </summary>
public class AgentScopeException : System.Exception
{
    /// <summary>
    /// Creates a new instance of AgentScopeException.
    /// 创建 AgentScopeException 的新实例
    /// </summary>
    public AgentScopeException() { }

    /// <summary>
    /// Creates a new instance of AgentScopeException with a message.
    /// 使用消息创建 AgentScopeException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    public AgentScopeException(string message) : base(message) { }

    /// <summary>
    /// Creates a new instance of AgentScopeException with a message and inner exception.
    /// 使用消息和内部异常创建 AgentScopeException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    /// <param name="inner">Inner exception / 内部异常</param>
    public AgentScopeException(string message, System.Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when tool execution fails.
/// 工具执行错误异常
/// </summary>
public class ToolException : AgentScopeException
{
    /// <summary>
    /// Creates a new instance of ToolException.
    /// 创建 ToolException 的新实例
    /// </summary>
    public ToolException() { }

    /// <summary>
    /// Creates a new instance of ToolException with a message.
    /// 使用消息创建 ToolException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    public ToolException(string message) : base(message) { }

    /// <summary>
    /// Creates a new instance of ToolException with a message and inner exception.
    /// 使用消息和内部异常创建 ToolException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    /// <param name="inner">Inner exception / 内部异常</param>
    public ToolException(string message, System.Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when an agent encounters an error.
/// Agent 错误异常
/// </summary>
public class AgentException : AgentScopeException
{
    /// <summary>
    /// Creates a new instance of AgentException.
    /// 创建 AgentException 的新实例
    /// </summary>
    public AgentException() { }

    /// <summary>
    /// Creates a new instance of AgentException with a message.
    /// 使用消息创建 AgentException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    public AgentException(string message) : base(message) { }

    /// <summary>
    /// Creates a new instance of AgentException with a message and inner exception.
    /// 使用消息和内部异常创建 AgentException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    /// <param name="inner">Inner exception / 内部异常</param>
    public AgentException(string message, System.Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception thrown when memory operations fail.
/// 记忆错误异常
/// </summary>
public class MemoryException : AgentScopeException
{
    /// <summary>
    /// Creates a new instance of MemoryException.
    /// 创建 MemoryException 的新实例
    /// </summary>
    public MemoryException() { }

    /// <summary>
    /// Creates a new instance of MemoryException with a message.
    /// 使用消息创建 MemoryException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    public MemoryException(string message) : base(message) { }

    /// <summary>
    /// Creates a new instance of MemoryException with a message and inner exception.
    /// 使用消息和内部异常创建 MemoryException 的新实例
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    /// <param name="inner">Inner exception / 内部异常</param>
    public MemoryException(string message, System.Exception inner) : base(message, inner) { }
}
