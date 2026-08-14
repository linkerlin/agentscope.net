// Copyright 2024-2026 the original author or authors.
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
/// 阿里云 AgentRun 沙箱客户端创建选项。对标 Java AgentRunSandboxClientOptions。
/// </summary>
public sealed class AgentRunSandboxClientOptions
{
    /// <summary>AgentRun API Key（必需）。</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>阿里云账号 ID。</summary>
    public string AccountId { get; set; } = "";

    /// <summary>地域（如 cn-hangzhou）。</summary>
    public string Region { get; set; } = "";

    /// <summary>数据面基础地址（未显式设置时由 accountId+region 推导）。</summary>
    public string? DataPlaneBaseUrl { get; set; }

    /// <summary>模板名称（必需）。</summary>
    public string TemplateName { get; set; } = "";

    /// <summary>MCP 服务端地址（必需）。</summary>
    public string McpServerUrl { get; set; } = "";

    /// <summary>MCP 端点路径。</summary>
    public string McpEndpoint { get; set; } = "/mcp";

    /// <summary>沙箱空闲超时（秒）。</summary>
    public int SandboxIdleTimeoutSeconds { get; set; } = 1800;

    /// <summary>容器内工作区根路径。</summary>
    public string WorkspaceRoot { get; set; } = AgentRunSandboxState.DefaultWorkspaceRoot;

    /// <summary>连接超时（秒）。</summary>
    public int ConnectTimeoutSeconds { get; set; } = 30;

    /// <summary>读取超时（秒）。</summary>
    public int ReadTimeoutSeconds { get; set; } = 120;

    /// <summary>最大重试次数。</summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>解析后的数据面基础地址（无尾部斜杠）。</summary>
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
