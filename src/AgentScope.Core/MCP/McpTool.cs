// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tool;

namespace AgentScope.Core.MCP;

/// <summary>
/// 将 MCP 工具封装为 ITool，供 Agent 调用。
/// </summary>
public class McpTool : ToolBase
{
    private readonly IMcpClient _client;
    private readonly McpToolSchema _schema;
    private readonly string _remoteToolName;
    private readonly McpContentConverter _contentConverter;
    private readonly McpErrorMapper _errorMapper;

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

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        try
        {
            if (!_client.IsInitialized)
            {
                await _client.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            }

            var result = await _client.CallToolAsync(
                    _remoteToolName,
                    parameters ?? new Dictionary<string, object>(),
                    CancellationToken.None)
                .ConfigureAwait(false);

            var output = _contentConverter.ConvertResultToText(result);
            if (result.IsError)
            {
                return ToolResult.Fail(_errorMapper.MapToolFailure(Name, _client.Name, output));
            }

            return ToolResult.Ok(output);
        }
        catch (global::System.Exception ex)
        {
            return _errorMapper.MapToolException(ex, Name, _client.Name);
        }
    }

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
