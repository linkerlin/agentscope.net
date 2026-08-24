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
/// MCP (Model Context Protocol) client abstraction.
/// Defines the contract for initializing, listing tools, and calling tools on an MCP server.
/// Can be implemented by the C# MCP SDK or a custom implementation.
/// Corresponds to Java: io.agentscope.core.mcp.McpClient
/// MCP（模型上下文协议）客户端抽象：定义初始化、列举工具和调用工具的契约。
/// 可由 C# MCP SDK 或自研实现。
/// 对应 Java: io.agentscope.core.mcp.McpClient
/// </summary>
public interface IMcpClient : IDisposable
{
    /// <summary>
    /// Gets the name of this MCP client (e.g., "file-server", "weather-api").
    /// 获取此 MCP 客户端的名称（例如 "file-server"、"weather-api"）。
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets whether the client has been successfully initialized.
    /// 获取客户端是否已成功初始化。
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Initializes the MCP client connection asynchronously.
    /// Must be called before any other operations.
    /// 异步初始化 MCP 客户端连接。必须在其他操作之前调用。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all available tools from the MCP server asynchronously.
    /// 异步列出 MCP 服务器上所有可用的工具。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>A read-only list of tool schemas / 工具架构的只读列表</returns>
    Task<IReadOnlyList<McpToolSchema>> ListToolsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Calls a specific tool on the MCP server asynchronously.
    /// 异步调用 MCP 服务器上的特定工具。
    /// </summary>
    /// <param name="toolName">The name of the tool to call / 要调用的工具名称</param>
    /// <param name="args">The arguments to pass to the tool / 传递给工具的参数</param>
    /// <param name="cancellationToken">Cancellation token for the async operation / 异步操作的取消令牌</param>
    /// <returns>The result of the tool call / 工具调用的结果</returns>
    Task<McpCallResult> CallToolAsync(string toolName, Dictionary<string, object> args, CancellationToken cancellationToken = default);
}
