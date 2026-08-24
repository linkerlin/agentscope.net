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
/// MCP tool registration record: tracks discovered client tools and their runtime exposed names.
/// Corresponds to Java: io.agentscope.core.mcp.McpToolRegistration
/// MCP 工具注册信息：记录发现到的客户端工具及其运行时暴露名。
/// 对应 Java: io.agentscope.core.mcp.McpToolRegistration
/// </summary>
public class McpToolRegistration
{
    /// <summary>
    /// Name of the MCP client that owns this tool / 拥有此工具的 MCP 客户端名称
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Name exposed to the agent at runtime / 运行时向 Agent 暴露的名称
    /// </summary>
    public string ExposedName { get; set; } = string.Empty;

    /// <summary>
    /// Original tool name on the remote server / 远程服务器上的原始工具名称
    /// </summary>
    public string RemoteName { get; set; } = string.Empty;

    /// <summary>
    /// Full tool schema definition / 完整的工具模式定义
    /// </summary>
    public McpToolSchema Schema { get; set; } = new();
}
