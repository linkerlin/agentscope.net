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
/// Wraps an MCP tool as an ITool for agent invocation.
/// 将 MCP 工具封装为 ITool，供 Agent 调用。
/// </summary>
public class McpTool : ToolBase
{
    /// <summary>MCP client for remote tool invocation / 用于远程工具调用的 MCP 客户端</summary>
    private readonly IMcpClient _client;

    /// <summary>Tool schema from the MCP server / 来自 MCP 服务器的工具模式</summary>
    private readonly McpToolSchema _schema;

    /// <summary>Original tool name on the remote server / 远程服务器上的原始工具名称</summary>
    private readonly string _remoteToolName;

    /// <summary>Converts MCP call results to text / 将 MCP 调用结果转换为文本</summary>
    private readonly McpContentConverter _contentConverter;

    /// <summary>Maps MCP errors to ToolResult failures / 将 MCP 错误映射为 ToolResult 失败</summary>
    private readonly McpErrorMapper _errorMapper;

    /// <summary>
    /// Initializes a new instance of <see cref="McpTool"/>.
    /// 初始化 McpTool 的新实例。
    /// </summary>
    /// <param name="client">MCP client / MCP 客户端</param>
    /// <param name="schema">Tool schema / 工具模式</param>
    /// <param name="exposedName">Name exposed to the agent (defaults to schema.Name) / 向 Agent 暴露的名称（默认为 schema.Name）</param>
    /// <param name="remoteToolName">Original name on the remote server (defaults to schema.Name) / 远程服务器上的原始名称（默认为 schema.Name）</param>
    /// <param name="contentConverter">Content converter / 内容转换器</param>
    /// <param name="errorMapper">Error mapper / 错误映射器</param>
    public McpTool(
        IMcpClient client,
        McpToolSchema schema,
        string? exposedName = null,
        string? remoteToolName = null,
        McpContentConverter? contentConverter = null,
        McpErrorMapper? errorMapper = null)
        : base(exposedName ?? schema.Name, schema.Description ?? "")
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        _remoteToolName = string.IsNullOrWhiteSpace(remoteToolName) ? schema.Name : remoteToolName;
        _contentConverter = contentConverter ?? new McpContentConverter();
        _errorMapper = errorMapper ?? new McpErrorMapper();
    }

    /// <summary>
    /// Executes the MCP tool with the given parameters.
    /// 使用给定参数执行 MCP 工具。
    /// </summary>
    /// <param name="parameters">Tool parameters / 工具参数</param>
    /// <returns>Tool execution result / 工具执行结果</returns>
    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        try
        {
            // Auto-initialize if not yet initialized / 未初始化时自动初始化
            if (!_client.IsInitialized)
            {
                await _client.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            }

            // Call the remote tool via MCP / 通过 MCP 调用远程工具
            var result = await _client.CallToolAsync(
                    _remoteToolName,
                    parameters ?? new Dictionary<string, object>(),
                    CancellationToken.None)
                .ConfigureAwait(false);

            // Convert result and handle error if any / 转换结果，如有错误则处理
            var output = _contentConverter.ConvertResultToText(result);
            if (result.IsError)
            {
                return ToolResult.Fail(_errorMapper.MapToolFailure(Name, _client.Name, output));
            }

            return ToolResult.Ok(output);
        }
        catch (global::System.Exception ex)
        {
            // Wrap any unexpected exception / 包装任何意外异常
            return _errorMapper.MapToolException(ex, Name, _client.Name);
        }
    }

    /// <summary>
    /// Returns the JSON schema of the tool for agent registration.
    /// 返回工具 JSON 模式，用于 Agent 注册。
    /// </summary>
    /// <returns>Tool schema dictionary / 工具模式字典</returns>
    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = _schema.InputSchema ?? new Dictionary<string, object>()
        };
    }
}
