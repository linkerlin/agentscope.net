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

using System.Text.Json;

namespace AgentScope.Extensions.Nacos.Prompt;

/// <summary>
/// Repository for managing prompts stored in Nacos configuration center.
/// Uses the Nacos Open API to get and publish prompt configurations.
/// 基于 Nacos 配置中心的 Prompt 仓库。通过 Nacos Open API 获取和发布 Prompt 配置。
/// </summary>
public sealed class NacosPromptRepository
{
    /// <summary>
    /// Nacos server address.
    /// Nacos 服务器地址。
    /// </summary>
    private readonly string _serverAddr;

    /// <summary>
    /// Nacos namespace (tenant) ID.
    /// Nacos 命名空间（租户）ID。
    /// </summary>
    private readonly string _namespaceId;

    /// <summary>
    /// Nacos configuration group.
    /// Nacos 配置分组。
    /// </summary>
    private readonly string _group;

    /// <summary>
    /// HttpClient for Nacos API calls.
    /// 用于 Nacos API 调用的 HttpClient。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// Initializes a new instance of the NacosPromptRepository.
    /// 初始化 NacosPromptRepository 的新实例。
    /// </summary>
    /// <param name="serverAddr">Nacos server address / Nacos 服务器地址</param>
    /// <param name="namespaceId">Nacos namespace (tenant) ID, defaults to "public" / Nacos 命名空间 ID，默认为 "public"</param>
    /// <param name="group">Nacos configuration group, defaults to "DEFAULT_GROUP" / Nacos 配置分组，默认为 "DEFAULT_GROUP"</param>
    /// <param name="http">Optional HttpClient instance / 可选的 HttpClient 实例</param>
    public NacosPromptRepository(string serverAddr, string? namespaceId = null, string? group = null, HttpClient? http = null)
    {
        _serverAddr = serverAddr.TrimEnd('/');
        _namespaceId = namespaceId ?? "public";
        _group = group ?? "DEFAULT_GROUP";
        _http = http ?? new HttpClient();
    }

    /// <summary>
    /// Retrieves a prompt by its ID from the Nacos configuration center.
    /// 从 Nacos 配置中心根据 ID 获取 Prompt。
    /// </summary>
    /// <param name="promptId">The prompt ID (dataId) / Prompt ID（dataId）</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>The prompt content, or null if not found / Prompt 内容，未找到时返回 null</returns>
    public async Task<string?> GetPromptAsync(string promptId, CancellationToken ct = default)
    {
        var url = $"{_serverAddr}/nacos/v1/cs/configs?dataId={promptId}&group={_group}&tenant={_namespaceId}";
        var resp = await _http.GetAsync(url, ct);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync(ct) : null;
    }

    /// <summary>
    /// Publishes a prompt to the Nacos configuration center.
    /// 向 Nacos 配置中心发布一个 Prompt。
    /// </summary>
    /// <param name="promptId">The prompt ID (dataId) / Prompt ID（dataId）</param>
    /// <param name="content">The prompt content / Prompt 内容</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async Task PublishPromptAsync(string promptId, string content, CancellationToken ct = default)
    {
        var url = $"{_serverAddr}/nacos/v1/cs/configs";
        var form = new Dictionary<string, string>
        {
            ["dataId"] = promptId,
            ["group"] = _group,
            ["tenant"] = _namespaceId,
            ["content"] = content
        };
        var resp = await _http.PostAsync(url, new FormUrlEncodedContent(form), ct);
        resp.EnsureSuccessStatusCode();
    }
}
