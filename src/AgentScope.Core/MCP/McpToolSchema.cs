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
/// MCP tool definition, corresponding to the Tool object returned by the MCP ListTools protocol.
/// Corresponds to Java: io.agentscope.core.mcp.McpToolSchema
/// MCP 工具定义（与 MCP 协议 ListTools 返回的 Tool 对应）。
/// 对应 Java: io.agentscope.core.mcp.McpToolSchema
/// </summary>
public class McpToolSchema
{
    /// <summary>
    /// Tool name / 工具名称
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Tool description / 工具描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// JSON Schema for tool input parameters / 工具输入参数的 JSON Schema
    /// </summary>
    public Dictionary<string, object>? InputSchema { get; set; }
}
