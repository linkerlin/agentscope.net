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

namespace AgentScope.Extensions.Channel.DingTalk;

/// <summary>
/// 拉取并缓存钉钉 OpenAPI 的 <c>accessToken</c>（按 appKey + appSecret 一对）。
/// Token 有效期约 7200 秒，本提供者在约 80% TTL 时主动刷新。
/// 对应 Java: io.agentscope.extensions.channel.dingtalk.DingTalkAccessTokenProvider
/// </summary>
/// <remarks>使用新 OpenAPI 端点 <c>POST /v1.0/oauth2/accessToken</c>。</remarks>
public sealed class DingTalkAccessTokenProvider
{
    private readonly HttpClient _http;
    private readonly string _apiBase;
    private readonly string _appKey;
    private readonly string _appSecret;
    private readonly object _lock = new();
    private TokenSlot _slot = TokenSlot.Empty;

    public DingTalkAccessTokenProvider(HttpClient http, string apiBase, string appKey, string appSecret)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _apiBase = (apiBase ?? throw new ArgumentNullException(nameof(apiBase))).TrimEnd('/');
        _appKey = appKey ?? throw new ArgumentNullException(nameof(appKey));
        _appSecret = appSecret ?? throw new ArgumentNullException(nameof(appSecret));
    }

    /// <summary>返回有效 access token；缓存缺失或临近过期时原地刷新。</summary>
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
        var payload = new { appKey = _appKey, appSecret = _appSecret };
        using var resp = await _http
            .PostAsync($"{_apiBase}/v1.0/oauth2/accessToken", JsonContent.Create(payload), ct)
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
            throw new InvalidOperationException("Failed to parse DingTalk accessToken response: " + e.Message, e);
        }
        if (node is null)
        {
            throw new InvalidOperationException("DingTalk accessToken response is empty: " + body);
        }

        var token = TextValue(node, "accessToken");
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("DingTalk accessToken response missing accessToken: " + body);
        }
        int expiresIn = IntValue(node, "expireIn", 7200);
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
