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

    public McpTool(IMcpClient client, McpToolSchema schema)
        : base(schema.Name, schema.Description ?? "")
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    public override async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!_client.IsInitialized)
        {
            try
            {
                await _client.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                return ToolResult.Fail("MCP 客户端未初始化: " + ex.Message);
            }
        }
        var result = await _client.CallToolAsync(Name, parameters, CancellationToken.None).ConfigureAwait(false);
        if (result.IsError)
            return ToolResult.Fail(result.Content ?? "MCP 调用失败");
        var output = result.Content;
        if (output == null && result.Parts != null && result.Parts.Count > 0)
            output = string.Join("\n", result.Parts.Where(p => !string.IsNullOrEmpty(p.Text)).Select(p => p!.Text));
        return ToolResult.Ok(output ?? "");
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
