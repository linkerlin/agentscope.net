// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tool;

namespace AgentScope.Core.MCP;

/// <summary>
/// MCP 运行时管理器：管理多个客户端，发现工具并物化为 ITool。
/// </summary>
public sealed class McpManager : IDisposable
{
    private readonly Dictionary<string, IMcpClient> _clients = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, McpToolEntry> _toolEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly McpContentConverter _contentConverter;
    private readonly McpErrorMapper _errorMapper;

    public McpManager(McpContentConverter? contentConverter = null, McpErrorMapper? errorMapper = null)
    {
        _contentConverter = contentConverter ?? new McpContentConverter();
        _errorMapper = errorMapper ?? new McpErrorMapper();
    }

    public void RegisterClient(IMcpClient client)
    {
        if (client == null)
        {
            throw new ArgumentNullException(nameof(client));
        }

        if (string.IsNullOrWhiteSpace(client.Name))
        {
            throw new ArgumentException("MCP client name 不能为空", nameof(client));
        }

        _clients[client.Name] = client;
    }

    public IReadOnlyList<string> GetClientNames()
    {
        return _clients.Keys.ToList();
    }

    public IReadOnlyList<McpToolRegistration> GetToolRegistrations()
    {
        return _toolEntries.Values.Select(static entry => entry.Registration).ToList();
    }

    public McpToolRegistration? GetTool(string toolName)
    {
        return _toolEntries.TryGetValue(toolName ?? string.Empty, out var entry)
            ? entry.Registration
            : null;
    }

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
                throw _errorMapper.MapException(ex, "发现工具", client.Name);
            }
        }

        return GetToolRegistrations();
    }

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
                // 忽略客户端清理异常。
            }
        }

        _clients.Clear();
        _toolEntries.Clear();
    }

    private void RegisterTool(IMcpClient client, McpToolSchema toolSchema)
    {
        if (toolSchema == null)
        {
            throw new McpException($"MCP 客户端 '{client.Name}' 返回了空工具定义");
        }

        var remoteName = toolSchema.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            throw new McpException($"MCP 客户端 '{client.Name}' 返回了缺少名称的工具定义");
        }

        if (_toolEntries.TryGetValue(remoteName, out var existing))
        {
            throw new McpException(
                $"MCP 工具名冲突: '{remoteName}' 同时来自 '{existing.Client.Name}' 和 '{client.Name}'。请在更高层做命名隔离。");
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

    private sealed record McpToolEntry(IMcpClient Client, McpToolRegistration Registration);
}