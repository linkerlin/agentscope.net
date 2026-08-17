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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentScope.Core.Message;
using AgentScope.Extensions.Channel;
using AgentScope.Extensions.Channel.Common;

namespace AgentScope.Extensions.Channel.Feishu;

/// <summary>
/// 飞书消息渠道。对标 Java FeishuChannel。
/// 通过飞书开放平台 Webhook 发送消息，并处理事件订阅回调（入站）。
/// </summary>
public sealed class FeishuChannel : IChannel
{
    /// <summary>HTTP client for calling Feishu OpenAPI / 用于调用飞书开放平台 API 的 HTTP 客户端</summary>
    private readonly HttpClient _http;

    /// <summary>Webhook URL for sending messages / 用于发送消息的 Webhook 地址</summary>
    private readonly string _webhookUrl;

    /// <summary>Feishu app secret / 飞书应用密钥</summary>
    private readonly string? _appSecret;

    /// <summary>Feishu app id / 飞书应用 app_id</summary>
    private readonly string? _appId;

    /// <summary>Optional token provider for API calls / 可选的 API 调用 token 提供者</summary>
    private readonly FeishuAccessTokenProvider? _tokenProvider;

    /// <summary>Optional crypto handler for encrypted callbacks / 可选的加密回调处理</summary>
    private readonly FeishuCrypto? _crypto;

    /// <summary>Optional verification token for URL challenge / 可选的 URL 校验验证令牌</summary>
    private readonly string? _verificationToken;

    /// <summary>Inbound message mapper / 入站消息映射器</summary>
    private readonly FeishuInboundMapper _mapper;

    /// <summary>Idempotency deduplication store / 幂等去重存储</summary>
    private readonly IdempotencyStore _idempotency = new();

    /// <summary>Bot loop guard to prevent infinite bot-bot loops / 防止机器人无限对话循环的守卫</summary>
    private readonly BotLoopGuard _botLoopGuard = new();

    /// <summary>Channel name identifier / 渠道名称标识</summary>
    public string Name => "feishu";

    /// <summary>Event raised when an inbound message is received / 收到入站消息时触发的事件</summary>
    public event Func<InboundMessage, Task>? OnMessageReceived;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeishuChannel"/> class.
    /// 初始化 <see cref="FeishuChannel"/> 类的新实例。
    /// </summary>
    /// <param name="http">HTTP client / HTTP 客户端</param>
    /// <param name="webhookUrl">Webhook URL for outgoing messages / 出站消息的 Webhook 地址</param>
    /// <param name="appSecret">Optional app secret for token-based API calls / 可选的应用密钥</param>
    /// <param name="appId">Optional app id for token-based API calls / 可选的 app_id</param>
    /// <param name="encryptKey">Optional encrypt key for callback decryption / 可选的回调解密加密密钥</param>
    /// <param name="verificationToken">Optional verification token for URL challenge / 可选的 URL 校验验证令牌</param>
    /// <param name="apiBase">Optional custom API base URL / 可选的自定义 API 基础地址</param>
    public FeishuChannel(
        HttpClient http,
        string webhookUrl,
        string? appSecret = null,
        string? appId = null,
        string? encryptKey = null,
        string? verificationToken = null,
        string? apiBase = null)
    {
        _http = http;
        _webhookUrl = webhookUrl;
        _appSecret = appSecret;
        _appId = appId;
        if (appId is not null && appSecret is not null)
        {
            _tokenProvider = new FeishuAccessTokenProvider(
                http, apiBase ?? "https://open.feishu.cn", appId, appSecret);
        }
        if (!string.IsNullOrWhiteSpace(encryptKey))
        {
            _crypto = new FeishuCrypto(encryptKey);
        }
        _verificationToken = verificationToken;
        _mapper = new FeishuInboundMapper(Name);
    }

    /// <summary>
    /// Gets the access token provider, available when appId/appSecret are configured.
    /// 获取 AccessToken 提供者（配置了 appId/appSecret 时可用）。
    /// </summary>
    public FeishuAccessTokenProvider? TokenProvider => _tokenProvider;

    /// <summary>
    /// Starts the channel. No-op for Feishu (stateless).
    /// 启动渠道。飞书渠道为无状态，无需操作。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>
    /// Stops the channel. No-op for Feishu (stateless).
    /// 停止渠道。飞书渠道为无状态，无需操作。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>
    /// Sends a text message via Feishu webhook.
    /// 通过飞书 Webhook 发送文本消息。
    /// </summary>
    /// <param name="message">The message to send / 要发送的消息</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async ValueTask SendAsync(Msg message, CancellationToken ct = default)
    {
        var payload = new
        {
            msg_type = "text",
            content = new { text = message.GetTextContent() ?? "" }
        };
        var json = JsonContent.Create(payload);
        using var resp = await _http.PostAsync(_webhookUrl, json, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Processes Feishu callback: optional signature verification → decryption → url_verification challenge → idempotency dedup (event_id) → mapping → BotLoopGuard → fire event.
    /// 处理飞书回调：可选验签 → 解密 → url_verification 挑战 → 幂等去重（event_id）→ 映射 → BotLoopGuard → 触发事件。
    /// </summary>
    /// <param name="rawBody">Raw request body / 原始请求体</param>
    /// <param name="headers">Request headers / 请求头</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>Processing result indicating verification and dispatch status / 处理结果，包含验证和分发状态</returns>
    public async ValueTask<InboundProcessResult> ProcessInboundAsync(
        string rawBody, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        var signature = Header(headers, "X-Lark-Signature");
        var timestamp = Header(headers, "X-Lark-Request-Timestamp");
        var nonce = Header(headers, "X-Lark-Request-Nonce");

        // 1. 可选验签（加密且提供了签名头时强制匹配）
        if (_crypto is not null && signature is not null
            && !_crypto.VerifySignature(signature, timestamp, nonce, rawBody))
        {
            return InboundProcessResult.FailedVerification;
        }

        // 2. 解析外层 body；若加密则解密后重新解析
        JsonNode? envelope;
        try
        {
            envelope = JsonNode.Parse(rawBody);
            if (envelope is not null && envelope["encrypt"] is not null && _crypto is not null)
            {
                var encrypt = TextValue(envelope, "encrypt");
                var plaintext = _crypto.Decrypt(encrypt ?? "");
                envelope = JsonNode.Parse(plaintext);
            }
        }
        catch (JsonException)
        {
            return InboundProcessResult.Dispatched([]);
        }
        catch (InvalidOperationException)
        {
            // 解密失败视为验证失败（拒绝）。
            return InboundProcessResult.FailedVerification;
        }
        if (envelope is null)
        {
            return InboundProcessResult.Dispatched([]);
        }

        // 3. URL 校验握手 —— 返回 {"challenge":"..."}
        var challenge = FeishuInboundMapper.ExtractUrlChallenge(envelope);
        if (challenge is not null)
        {
            var tokenFromBody = TextValue(envelope, "token");
            if (!string.IsNullOrWhiteSpace(_verificationToken)
                && !string.Equals(_verificationToken, tokenFromBody, StringComparison.Ordinal))
            {
                return InboundProcessResult.FailedVerification;
            }
            return new InboundProcessResult
            {
                Supported = true,
                Verified = true,
                Messages = [],
                ChallengeResponse = JsonSerializer.Serialize(new Dictionary<string, string> { ["challenge"] = challenge }),
            };
        }

        // 4. 幂等去重（event_id）
        var eventId = FeishuInboundMapper.ExtractEventId(envelope);
        if (eventId is not null && !_idempotency.FirstSeen($"{Name}|{eventId}"))
        {
            return InboundProcessResult.SkippedAsDuplicate;
        }

        // 5. 映射
        var inbound = _mapper.Map(envelope);
        if (inbound is null)
        {
            return InboundProcessResult.Dispatched([]);
        }

        // 6. BotLoopGuard（按会话 peer 限流）
        if (!_botLoopGuard.Allow(PeerKey(inbound.Value)))
        {
            return InboundProcessResult.Dispatched([]);
        }

        // 7. 触发事件
        await FireAsync(inbound.Value, ct).ConfigureAwait(false);

        return InboundProcessResult.Dispatched(new[] { inbound.Value });
    }

    /// <summary>
    /// Fires the OnMessageReceived event to all registered handlers.
    /// 向所有已注册的处理程序触发 OnMessageReceived 事件。
    /// </summary>
    /// <param name="message">The inbound message to dispatch / 要分发的入站消息</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    private async Task FireAsync(InboundMessage message, CancellationToken ct)
    {
        var handlers = OnMessageReceived;
        if (handlers is null)
        {
            return;
        }
        foreach (Func<InboundMessage, Task> handler in handlers.GetInvocationList())
        {
            ct.ThrowIfCancellationRequested();
            await handler(message).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Safely extracts a header value from the headers dictionary (case-insensitive).
    /// 安全地从请求头字典中提取 header 值（不区分大小写）。
    /// </summary>
    /// <param name="headers">Request headers dictionary / 请求头字典</param>
    /// <param name="name">Header name / 请求头名称</param>
    /// <returns>Header value, or null if not found / 请求头值，未找到时返回 null</returns>
    private static string? Header(IReadOnlyDictionary<string, string>? headers, string name)
    {
        if (headers is null)
        {
            return null;
        }
        foreach (var kv in headers)
        {
            if (string.Equals(kv.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return kv.Value;
            }
        }
        return null;
    }

    /// <summary>
    /// Extracts the peer key from an inbound message for bot loop guard.
    /// 从入站消息中提取 peer 键，用于机器人循环防护。
    /// </summary>
    /// <param name="message">The inbound message / 入站消息</param>
    /// <returns>Peer identifier string / 对端标识字符串</returns>
    private static string PeerKey(InboundMessage message)
    {
        if (message.Metadata is { } md && md.TryGetValue("peer", out var peer) && peer is not null)
        {
            return peer.ToString() ?? message.From;
        }
        return message.From;
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
}
