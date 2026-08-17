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

using AgentScope.Core.Tool;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP runtime manager: manages multiple clients, discovers tools, and materializes them as ITool instances.
/// Corresponds to Java: io.agentscope.core.mcp.McpManager
/// MCP 运行时管理器：管理多个客户端，发现工具并物化为 ITool。
/// 对应 Java: io.agentscope.core.mcp.McpManager
/// </summary>
public sealed class McpManager : IDisposable
{
    private readonly Dictionary<string, IMcpClient> _clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, McpToolEntry> _toolEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly McpContentConverter _contentConverter;
    private readonly McpErrorMapper _errorMapper;

    /// <summary>
    /// Initializes a new instance of <see cref="McpManager"/>.
    /// 初始化 McpManager 的新实例。
    /// </summary>
    /// <param name="contentConverter">Optional content converter / 可选的内容转换器</param>
    /// <param name="errorMapper">Optional error mapper / 可选的错误映射器</param>
    public McpManager(McpContentConverter? contentConverter = null, McpErrorMapper? errorMapper = null)
    {
        _contentConverter = contentConverter ?? new McpContentConverter();
        _errorMapper = errorMapper ?? new McpErrorMapper();
    }

    /// <summary>
    /// Registers an MCP client for tool discovery and invocation.
    /// 注册一个 MCP 客户端用于工具发现和调用。
    /// </summary>
    /// <param name="client">The MCP client to register / 要注册的 MCP 客户端</param>
    /// <exception cref="ArgumentNullException">Thrown when client is null / 客户端为 null 时抛出</exception>
    /// <exception cref="ArgumentException">Thrown when client name is empty / 客户端名称为空时抛出</exception>
    public void RegisterClient(IMcpClient client)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (string.IsNullOrWhiteSpace(client.Name))
        {
            throw new ArgumentException("MCP client name cannot be empty / MCP 客户端名称不能为空", nameof(client));
        }

        _clients[client.Name] = client;
    }

    /// <summary>
    /// Gets the list of registered client names.
    /// 获取已注册的客户端名称列表。
    /// </summary>
    /// <returns>List of client names / 客户端名称列表</returns>
    public IReadOnlyList<string> GetClientNames()
    {
        return _clients.Keys.ToList();
    }

    /// <summary>
    /// Gets all discovered tool registrations.
    /// 获取所有已发现的工具注册信息。
    /// </summary>
    /// <returns>List of tool registrations / 工具注册信息列表</returns>
    public IReadOnlyList<McpToolRegistration> GetToolRegistrations()
    {
        return _toolEntries.Values.Select(static entry => entry.Registration).ToList();
    }

    /// <summary>
    /// Gets a specific tool registration by name.
    /// 根据名称获取特定的工具注册信息。
    /// </summary>
    /// <param name="toolName">The tool name / 工具名称</param>
    /// <returns>The tool registration, or null if not found / 工具注册信息，未找到则返回 null</returns>
    public McpToolRegistration? GetTool(string toolName)
    {
        return _toolEntries.TryGetValue(toolName ?? string.Empty, out var entry)
            ? entry.Registration
            : null;
    }

    /// <summary>
    /// Discovers tools from all registered MCP clients asynchronously.
    /// Initializes uninitialized clients and collects their tool schemas.
    /// 异步从所有已注册的 MCP 客户端发现工具。
    /// 初始化未初始化的客户端并收集其工具架构。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>List of discovered tool registrations / 发现的工具注册信息列表</returns>
    public async Task<IReadOnlyList<McpToolRegistration>> DiscoverToolsAsync(CancellationToken cancellationToken = default)
    {
        _toolEntries.Clear();

        foreach (var client in _clients.Values)
        {
            try
            {
                if (!client.IsInitialized)
                {
                    await client.InitializeAsync(cancellationToken).ConfigureAwait(false);
                }

                var toolSchemas = await client.ListToolsAsync(cancellationToken).ConfigureAwait(false);
                foreach (var toolSchema in toolSchemas)
                {
                    RegisterTool(client, toolSchema);
                }
            }
            catch (global::System.Exception ex)
            {
                throw _errorMapper.MapException(ex, "discovering tools / 发现工具", client.Name);
            }
        }

        return GetToolRegistrations();
    }

    /// <summary>
    /// Creates ITool instances from all discovered tool registrations asynchronously.
    /// Automatically discovers tools if none have been discovered yet.
    /// 异步从所有已发现的工具注册信息创建 ITool 实例。
    /// 如果尚未发现工具，则自动执行发现。
    /// </summary>
    /// <param name="cancellationToken">Cancellation token / 取消令牌</param>
    /// <returns>List of ITool instances / ITool 实例列表</returns>
    public async Task<IReadOnlyList<ITool>> CreateToolsAsync(CancellationToken cancellationToken = default)
    {
        if (_toolEntries.Count == 0)
        {
            await DiscoverToolsAsync(cancellationToken).ConfigureAwait(false);
        }

        return _toolEntries.Values
            .Select(CreateTool)
            .Cast<ITool>()
            .ToList();
    }

    /// <summary>
    /// Disposes all registered MCP clients and clears internal state.
    /// 释放所有已注册的 MCP 客户端并清除内部状态。
    /// </summary>
    public void Dispose()
    {
        foreach (var client in _clients.Values)
        {
            try
            {
                client.Dispose();
            }
            catch
            {
                // Ignore client cleanup exceptions / 忽略客户端清理异常
            }
        }

        _clients.Clear();
        _toolEntries.Clear();
    }

    /// <summary>
    /// Registers a tool from a client's tool schema, performing validation and conflict detection.
    /// 从客户端的工具架构注册工具，执行验证和冲突检测。
    /// </summary>
    /// <param name="client">The MCP client that owns the tool / 拥有该工具的 MCP 客户端</param>
    /// <param name="toolSchema">The tool schema from the server / 来自服务器的工具架构</param>
    /// <exception cref="McpException">Thrown when tool schema is invalid or name conflicts / 工具架构无效或名称冲突时抛出</exception>
    private void RegisterTool(IMcpClient client, McpToolSchema toolSchema)
    {
        if (toolSchema == null)
        {
            throw new McpException($"MCP client '{client.Name}' returned a null tool definition / MCP 客户端 '{client.Name}' 返回了空工具定义");
        }

        var remoteName = toolSchema.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            throw new McpException($"MCP client '{client.Name}' returned a tool definition without a name / MCP 客户端 '{client.Name}' 返回了缺少名称的工具定义");
        }

        if (_toolEntries.TryGetValue(remoteName, out var existing))
        {
            throw new McpException(
                $"MCP tool name conflict: '{remoteName}' from both '{existing.Client.Name}' and '{client.Name}'. Apply namespace isolation at a higher level. / MCP 工具名冲突: '{remoteName}' 同时来自 '{existing.Client.Name}' 和 '{client.Name}'。请在更高层做命名隔离。");
        }

        var schemaCopy = new McpToolSchema
        {
            Name = remoteName,
            Description = toolSchema.Description,
            InputSchema = toolSchema.InputSchema
        };

        _toolEntries[remoteName] = new McpToolEntry(
            client,
            new McpToolRegistration
            {
                ClientName = client.Name,
                ExposedName = remoteName,
                RemoteName = remoteName,
                Schema = schemaCopy
            });
    }

    /// <summary>
    /// Creates an McpTool instance from a tool entry.
    /// 从工具条目创建 McpTool 实例。
    /// </summary>
    /// <param name="entry">The tool entry containing client and registration info / 包含客户端和注册信息的工具条目</param>
    /// <returns>An McpTool instance / McpTool 实例</returns>
    private McpTool CreateTool(McpToolEntry entry)
    {
        return new McpTool(
            entry.Client,
            entry.Registration.Schema,
            entry.Registration.ExposedName,
            entry.Registration.RemoteName,
            _contentConverter,
            _errorMapper);
    }

    /// <summary>
    /// Internal record linking an MCP client with its tool registration.
    /// 内部记录，将 MCP 客户端与其工具注册信息关联。
    /// </summary>
    private sealed record McpToolEntry(IMcpClient Client, McpToolRegistration Registration);
}