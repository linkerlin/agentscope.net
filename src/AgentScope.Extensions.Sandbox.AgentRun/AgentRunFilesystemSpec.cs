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

namespace AgentScope.Extensions.Sandbox.AgentRun;

/// <summary>
/// Alibaba Cloud AgentRun sandbox filesystem specification.
/// Describes the container working directory and data plane/template configuration.
/// Counterpart of Java AgentRunFilesystemSpec.
/// <br/>
/// 阿里云 AgentRun 沙箱的文件系统规格。
/// 描述容器内工作目录与数据面/模板配置。对标 Java AgentRunFilesystemSpec。
/// </summary>
/// <param name="ContainerWorkspace">Container workspace root path / 容器内工作区根路径</param>
/// <param name="TemplateName">AgentRun template name / AgentRun 模板名称</param>
/// <param name="AccountId">Alibaba Cloud account ID / 阿里云账号 ID</param>
/// <param name="Region">Alibaba Cloud region (e.g. cn-hangzhou) / 阿里云地域</param>
/// <param name="DataPlaneBaseUrl">Data plane base URL (auto-derived if null) / 数据面基础地址</param>
/// <param name="McpServerUrl">MCP server URL / MCP 服务端地址</param>
/// <param name="McpEndpoint">MCP endpoint path (default /mcp) / MCP 端点路径</param>
public sealed record AgentRunFilesystemSpec(
    string ContainerWorkspace = AgentRunSandboxState.DefaultWorkspaceRoot,
    string? TemplateName = null,
    string? AccountId = null,
    string? Region = null,
    string? DataPlaneBaseUrl = null,
    string? McpServerUrl = null,
    string McpEndpoint = "/mcp")
{
    /// <summary>
    /// The workspace root path inside the container.
    /// 容器内工作区根路径。
    /// </summary>
    public string WorkspaceRoot => ContainerWorkspace;
}
