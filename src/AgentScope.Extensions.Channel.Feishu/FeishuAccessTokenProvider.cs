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
    private readonly HttpClient _http;
    private readonly string _apiBase;
    private readonly string _appId;
    private readonly string _appSecret;
    private readonly object _lock = new();
    private TokenSlot _slot = TokenSlot.Empty;

    public FeishuAccessTokenProvider(HttpClient http, string apiBase, string appId, string appSecret)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _apiBase = (apiBase ?? throw new ArgumentNullException(nameof(apiBase))).TrimEnd('/');
        _appId = appId ?? throw new ArgumentNullException(nameof(appId));
        _appSecret = appSecret ?? throw new ArgumentNullException(nameof(appSecret));
    }

    /// <summary>返回有效 tenant_access_token；缓存缺失或临近过期时原地刷新。</summary>
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

    /// <summary>强制下一次 <see cref="GetTokenAsync"/> 刷新。</summary>
    public void Invalidate()
    {
        lock (_lock)
        {
            _slot = TokenSlot.Empty;
        }
    }

    private static string? TextValue(JsonNode? node, string field)
    {
        var v = node?[field];
        if (v is null)
        {
            return null;
        }
        return v.GetValueKind() == JsonValueKind.String ? v.GetValue<string>() : v.ToString();
    }

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

    private sealed record TokenSlot(string? Value, long RefreshAtMs)
    {
        public static readonly TokenSlot Empty = new(null, 0L);
    }
}
