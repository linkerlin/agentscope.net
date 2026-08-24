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
using AgentScope.Core.Message;
using AgentScope.Extensions.Channel;
using AgentScope.Extensions.Channel.Common;

namespace AgentScope.Extensions.Channel.DingTalk;

/// <summary>
/// 钉钉通信渠道。对标 Java DingTalkChannel。
/// 通过钉钉开放平台 API（Webhook + 回调）收发消息。
/// </summary>
public sealed class DingTalkChannel : IChannel
{
    /// <summary>HTTP client for calling DingTalk APIs / 用于调用钉钉 API 的 HTTP 客户端</summary>
    private readonly HttpClient _http;

    /// <summary>Webhook URL for sending messages / 用于发送消息的 Webhook 地址</summary>
    private readonly string _webhookUrl;

    /// <summary>DingTalk app secret / 钉钉应用密钥</summary>
    private readonly string? _appSecret;

    /// <summary>DingTalk app key / 钉钉应用 key</summary>
    private readonly string? _appKey;

    /// <summary>Optional token provider for API calls / 可选的 API 调用 token 提供者</summary>
    private readonly DingTalkAccessTokenProvider? _tokenProvider;

    /// <summary>Inbound message mapper / 入站消息映射器</summary>
    private readonly DingTalkInboundMapper _mapper;

    /// <summary>Idempotency deduplication store / 幂等去重存储</summary>
    private readonly IdempotencyStore _idempotency = new();

    /// <summary>Bot loop guard to prevent infinite bot-bot loops / 防止机器人无限对话循环的守卫</summary>
    private readonly BotLoopGuard _botLoopGuard = new();

    /// <summary>Channel name identifier / 渠道名称标识</summary>
    public string Name => "dingtalk";

    /// <summary>Event raised when an inbound message is received / 收到入站消息时触发的事件</summary>
    public event Func<InboundMessage, Task>? OnMessageReceived;

    /// <summary>
    /// Initializes a new instance of the <see cref="DingTalkChannel"/> class.
    /// 初始化 <see cref="DingTalkChannel"/> 类的新实例。
    /// </summary>
    /// <param name="http">HTTP client / HTTP 客户端</param>
    /// <param name="webhookUrl">Webhook URL for outgoing messages / 出站消息的 Webhook 地址</param>
    /// <param name="appSecret">Optional app secret for token-based API calls / 可选的应用密钥，用于基于 token 的 API 调用</param>
    /// <param name="appKey">Optional app key for token-based API calls / 可选的应用 key，用于基于 token 的 API 调用</param>
    /// <param name="apiBase">Optional custom API base URL / 可选的自定义 API 基础地址</param>
    public DingTalkChannel(
        HttpClient http,
        string webhookUrl,
        string? appSecret = null,
        string? appKey = null,
        string? apiBase = null)
    {
        _http = http;
        _webhookUrl = webhookUrl;
        _appSecret = appSecret;
        _appKey = appKey;
        if (appKey is not null && appSecret is not null)
        {
            _tokenProvider = new DingTalkAccessTokenProvider(
                http, apiBase ?? "https://api.dingtalk.com", appKey, appSecret);
        }
        _mapper = new DingTalkInboundMapper(Name, appKey ?? "");
    }

    /// <summary>
    /// Gets the access token provider, available when appKey/appSecret are configured.
    /// 获取 AccessToken 提供者（配置了 appKey/appSecret 时可用）。
    /// </summary>
    public DingTalkAccessTokenProvider? TokenProvider => _tokenProvider;

    /// <summary>
    /// Starts the channel. No-op for DingTalk (stateless).
    /// 启动渠道。钉钉渠道为无状态，无需操作。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>
    /// Stops the channel. No-op for DingTalk (stateless).
    /// 停止渠道。钉钉渠道为无状态，无需操作。
    /// </summary>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

    /// <summary>
    /// Sends a text message via DingTalk webhook.
    /// 通过钉钉 Webhook 发送文本消息。
    /// </summary>
    /// <param name="message">The message to send / 要发送的消息</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async ValueTask SendAsync(Msg message, CancellationToken ct = default)
    {
        var payload = new
        {
            msgtype = "text",
            text = new { content = message.GetTextContent() ?? "" }
        };
        var json = JsonContent.Create(payload);
        await _http.PostAsync(_webhookUrl, json, ct);
    }

    /// <summary>
    /// Processes DingTalk inbound message callback: idempotency dedup (msgId) → mapping → BotLoopGuard → fire event.
    /// DingTalk webhook callbacks have no signature verification.
    /// 处理钉钉入站消息回调：幂等去重（msgId）→ 映射 → BotLoopGuard → 触发事件。
    /// 钉钉 webhook 回调无签名校验。
    /// </summary>
    /// <param name="rawBody">Raw request body / 原始请求体</param>
    /// <param name="headers">Request headers / 请求头</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>Processing result indicating verification and dispatch status / 处理结果，包含验证和分发状态</returns>
    public async ValueTask<InboundProcessResult> ProcessInboundAsync(
        string rawBody, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        JsonNode? payload;
        try
        {
            payload = JsonNode.Parse(rawBody);
        }
        catch (JsonException)
        {
            // 无法解析的 payload 静默确认（无需派发）。
            return InboundProcessResult.Dispatched([]);
        }

        // 1. 幂等去重
        var msgId = DingTalkInboundMapper.ExtractMsgId(payload);
        if (msgId is not null && !_idempotency.FirstSeen($"{Name}|{msgId}"))
        {
            return InboundProcessResult.SkippedAsDuplicate;
        }

        // 2. 映射
        var inbound = _mapper.Map(payload);
        if (inbound is null)
        {
            return InboundProcessResult.Dispatched([]);
        }

        // 3. BotLoopGuard（按会话 peer 限流）
        if (!_botLoopGuard.Allow(PeerKey(inbound.Value)))
        {
            return InboundProcessResult.Dispatched([]);
        }

        // 4. 触发事件
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
}
