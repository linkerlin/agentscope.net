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

namespace AgentScope.Core.MCP;

/// <summary>
/// Abstract base class for MCP client wrappers.
/// Corresponds to Java: io.agentscope.core.mcp.McpClientWrapper
/// Concrete implementations can delegate to the C# MCP SDK or provide custom logic.
/// MCP 客户端包装器抽象基类。
/// 对应 Java: io.agentscope.core.mcp.McpClientWrapper
/// 具体实现可委托给 C# MCP SDK 或提供自定义逻辑。
/// </summary>
public abstract class McpClientWrapper : IMcpClient
{
    /// <summary>
    /// Gets the name of this MCP client.
    /// 获取此 MCP 客户端的名称。
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets whether the client has been initialized.
    /// 获取客户端是否已初始化。
    /// </summary>
    public virtual bool IsInitialized { get; protected set; }

    /// <summary>
    /// Initializes the MCP client asynchronously.
    /// 异步初始化 MCP 客户端。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>A task representing the asynchronous operation / 表示异步操作的任务</returns>
    public abstract Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists available tools from the MCP server asynchronously.
    /// 异步列出 MCP 服务器上可用的工具。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>A list of tool schemas / 工具架构列表</returns>
    public abstract Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a tool on the MCP server asynchronously.
    /// 异步调用 MCP 服务器上的工具。
    /// </summary>
    /// <param name="toolName">The name of the tool to call / 要调用的工具名称</param>
    /// <param name="args">The arguments for the tool / 工具参数</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>The tool call result / 工具调用结果</returns>
    public abstract Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases resources. Suppresses finalization.
    /// 释放资源。抑制终结器。
    /// </summary>
    public virtual void Dispose() => GC.SuppressFinalize(this);
}
