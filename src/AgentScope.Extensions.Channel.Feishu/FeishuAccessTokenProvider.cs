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

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AgentScope.Extensions.Channel.Feishu;

/// <summary>
/// 拉取并缓存飞书 <c>tenant_access_token</c>（按 app_id + app_secret 一对）。
/// Token 有效期约 7200 秒，本提供者在约 80% TTL 时主动刷新。
/// 对应 Java: io.agentscope.extensions.channel.feishu.FeishuAccessTokenProvider
/// </summary>
public sealed class FeishuAccessTokenProvider
{
    /// <summary>HTTP client for calling Feishu OpenAPI / 用于调用飞书开放平台 API 的 HTTP 客户端</summary>
    private readonly HttpClient _http;

    /// <summary>API base URL / API 基础地址</summary>
    private readonly string _apiBase;

    /// <summary>Feishu app id / 飞书应用的 app_id</summary>
    private readonly string _appId;

    /// <summary>Feishu app secret / 飞书应用的 app_secret</summary>
    private readonly string _appSecret;

    /// <summary>Synchronization lock for thread-safe token refresh / 用于线程安全刷新 token 的同步锁</summary>
    private readonly object _lock = new();

    /// <summary>Cached token slot / 缓存的 token 槽位</summary>
    private TokenSlot _slot = TokenSlot.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeishuAccessTokenProvider"/> class.
    /// 初始化 <see cref="FeishuAccessTokenProvider"/> 类的新实例。
    /// </summary>
    /// <param name="http">HTTP client / HTTP 客户端</param>
    /// <param name="apiBase">Feishu API base URL / 飞书 API 基础地址</param>
    /// <param name="appId">Feishu app id / 飞书 app_id</param>
    /// <param name="appSecret">Feishu app secret / 飞书 app_secret</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null / 任一参数为 null 时抛出</exception>
    public FeishuAccessTokenProvider(HttpClient http, string apiBase, string appId, string appSecret)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _apiBase = (apiBase ?? throw new ArgumentNullException(nameof(apiBase))).TrimEnd('/');
        _appId = appId ?? throw new ArgumentNullException(nameof(appId));
        _appSecret = appSecret ?? throw new ArgumentNullException(nameof(appSecret));
    }

    /// <summary>
    /// Returns a valid tenant_access_token; refreshes in-place when cache is missing or near expiry.
    /// 返回有效 tenant_access_token；缓存缺失或临近过期时原地刷新。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>A valid tenant_access_token string / 有效的 tenant_access_token 字符串</returns>
    public Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        var s = _slot;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        if (s.Value is not null && s.RefreshAtMs > now)
        {
            return Task.FromResult(s.Value);
        }
        return RefreshAsync(ct);
    }

    /// <summary>
    /// Actually refreshes the token by calling Feishu tenant_access_token API.
    /// 通过调用飞书 tenant_access_token API 实际刷新 token。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>A fresh tenant_access_token string / 新的 tenant_access_token 字符串</returns>
    private async Task<string> RefreshAsync(CancellationToken ct)
    {
        var payload = new { app_id = _appId, app_secret = _appSecret };
        using var resp = await _http
            .PostAsync($"{_apiBase}/open-apis/auth/v3/tenant_access_token/internal", JsonContent.Create(payload), ct)
            .ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return ParseAndStore(body);
    }

    /// <summary>
    /// Parses the token response JSON and stores the token in the cache.
    /// 解析 token 响应 JSON 并将 token 存入缓存。
    /// </summary>
    /// <param name="body">Raw JSON response body / 原始 JSON 响应体</param>
    /// <returns>The extracted tenant_access_token / 提取的 tenant_access_token</returns>
    /// <exception cref="InvalidOperationException">Thrown when parsing fails or token is missing / 解析失败或缺少 token 时抛出</exception>
    private string ParseAndStore(string body)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(body);
        }
        catch (JsonException e)
        {
            throw new InvalidOperationException("Failed to parse Feishu tenant_access_token response: " + e.Message, e);
        }
        if (node is null)
        {
            throw new InvalidOperationException("Feishu tenant_access_token response is empty: " + body);
        }

        int code = IntValue(node, "code", 0);
        if (code != 0)
        {
            throw new InvalidOperationException(
                "Feishu tenant_access_token failed: code=" + code + ", msg=" + TextValue(node, "msg"));
        }
        var token = TextValue(node, "tenant_access_token");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Feishu tenant_access_token response missing token: " + body);
        }
        int expiresIn = IntValue(node, "expire", 7200);
        long refreshAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (long)expiresIn * 800L;
        lock (_lock)
        {
            _slot = new TokenSlot(token, refreshAt);
        }
        return token;
    }

    /// <summary>
    /// Forces the next <see cref="GetTokenAsync"/> call to refresh the token.
    /// 强制下一次 <see cref="GetTokenAsync"/> 刷新。
    /// </summary>
    public void Invalidate()
    {
        lock (_lock)
        {
            _slot = TokenSlot.Empty;
        }
    }

    /// <summary>
    /// Safely extracts a string field from a JSON node.
    /// 安全地从 JSON 节点中提取字符串字段。
    /// </summary>
    /// <param name="node">Source JSON node / 源 JSON 节点</param>
    /// <param name="field">Field name / 字段名</param>
    /// <returns>The field value as string, or null if missing / 字段值的字符串形式，缺失时返回 null</returns>
    private static string? TextValue(JsonNode? node, string field)
    {
        var v = node?[field];
        if (v is null)
        {
            return null;
        }
        return v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : v.ToString();
    }

    /// <summary>
    /// Safely extracts an integer field from a JSON node with a fallback default.
    /// 安全地从 JSON 节点中提取整数字段，含默认值回退。
    /// </summary>
    /// <param name="node">Source JSON node / 源 JSON 节点</param>
    /// <param name="field">Field name / 字段名</param>
    /// <param name="fallback">Default value when field is missing or invalid / 字段缺失或无效时的默认值</param>
    /// <returns>The integer value or fallback / 整数值或回退默认值</returns>
    private static int IntValue(JsonNode? node, string field, int fallback)
    {
        var v = node?[field];
        if (v is null)
        {
            return fallback;
        }
        if (v.GetValueKind() == JsonValueKind.Number)
        {
            try
            {
                return v.GetValue<int>();
            }
            catch (InvalidOperationException)
            {
                // fall through to fallback
            }
        }
        else if (v.GetValueKind() == JsonValueKind.String && int.TryParse(v.GetValue<string>(), out var i))
        {
            return i;
        }
        return fallback;
    }

    /// <summary>
    /// Internal record holding a cached token and its refresh timestamp.
    /// 内部记录，保存缓存的 token 及其刷新时间戳。
    /// </summary>
    /// <param name="Value">The cached token value, or null when empty / 缓存的 token 值，为空时为 null</param>
    /// <param name="RefreshAtMs">Unix timestamp in milliseconds indicating when to refresh / 指示何时刷新的 Unix 毫秒时间戳</param>
    private sealed record TokenSlot(string? Value, long RefreshAtMs)
    {
        /// <summary>Empty slot singleton / 空槽位单例</summary>
        public static readonly TokenSlot Empty = new(null, 0L);
    }
}
