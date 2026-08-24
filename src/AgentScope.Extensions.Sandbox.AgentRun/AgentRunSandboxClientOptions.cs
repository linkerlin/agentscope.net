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
/// Alibaba Cloud AgentRun sandbox client creation options.
/// Counterpart of Java AgentRunSandboxClientOptions.
/// <br/>
/// 阿里云 AgentRun 沙箱客户端创建选项。对标 Java AgentRunSandboxClientOptions。
/// </summary>
public sealed class AgentRunSandboxClientOptions
{
    /// <summary>
    /// AgentRun API Key (required).
    /// AgentRun API Key（必需）。
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// Alibaba Cloud account ID.
    /// 阿里云账号 ID。
    /// </summary>
    public string AccountId { get; set; } = "";

    /// <summary>
    /// Alibaba Cloud region (e.g. cn-hangzhou).
    /// 地域（如 cn-hangzhou）。
    /// </summary>
    public string Region { get; set; } = "";

    /// <summary>
    /// Data plane base URL (auto-derived from accountId+region if not explicitly set).
    /// 数据面基础地址（未显式设置时由 accountId+region 推导）。
    /// </summary>
    public string? DataPlaneBaseUrl { get; set; }

    /// <summary>
    /// Template name (required).
    /// 模板名称（必需）。
    /// </summary>
    public string TemplateName { get; set; } = "";

    /// <summary>
    /// MCP server URL (required).
    /// MCP 服务端地址（必需）。
    /// </summary>
    public string McpServerUrl { get; set; } = "";

    /// <summary>
    /// MCP endpoint path.
    /// MCP 端点路径。
    /// </summary>
    public string McpEndpoint { get; set; } = "/mcp";

    /// <summary>
    /// Sandbox idle timeout in seconds.
    /// 沙箱空闲超时（秒）。
    /// </summary>
    public int SandboxIdleTimeoutSeconds { get; set; } = 1800;

    /// <summary>
    /// Container workspace root path.
    /// 容器内工作区根路径。
    /// </summary>
    public string WorkspaceRoot { get; set; } = AgentRunSandboxState.DefaultWorkspaceRoot;

    /// <summary>
    /// Connection timeout in seconds.
    /// 连接超时（秒）。
    /// </summary>
    public int ConnectTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Read timeout in seconds.
    /// 读取超时（秒）。
    /// </summary>
    public int ReadTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Maximum retry count.
    /// 最大重试次数。
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Resolved data plane base URL (without trailing slash).
    /// Auto-generated from AccountId and Region when DataPlaneBaseUrl is not set.
    /// <br/>
    /// 解析后的数据面基础地址（无尾部斜杠）。
    /// 当 DataPlaneBaseUrl 未设置时，由 AccountId 和 Region 自动生成。
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when neither DataPlaneBaseUrl nor AccountId+Region are properly configured.
    /// 当 DataPlaneBaseUrl 和 AccountId+Region 均未正确配置时抛出。
    /// </exception>
    public string ResolvedDataPlaneBaseUrl
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(DataPlaneBaseUrl))
                return DataPlaneBaseUrl!.TrimEnd('/');
            if (string.IsNullOrWhiteSpace(AccountId) || string.IsNullOrWhiteSpace(Region))
                throw new InvalidOperationException(
                    "AgentRun requires accountId+region or an explicit dataPlaneBaseUrl.");
            return $"https://{AccountId}.agentrun-data.{Region}.aliyuncs.com";
        }
    }
}
