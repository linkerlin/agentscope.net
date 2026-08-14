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
    private readonly HttpClient _http;
    private readonly string _webhookUrl;
    private readonly string? _appSecret;
    private readonly string? _appKey;
    private readonly DingTalkAccessTokenProvider? _tokenProvider;
    private readonly DingTalkInboundMapper _mapper;
    private readonly IdempotencyStore _idempotency = new();
    private readonly BotLoopGuard _botLoopGuard = new();

    public string Name => "dingtalk";
    public event Func<InboundMessage, Task>? OnMessageReceived;

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

    /// <summary>AccessToken 提供者（配置了 appKey/appSecret 时可用）。</summary>
    public DingTalkAccessTokenProvider? TokenProvider => _tokenProvider;

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

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
    /// 处理钉钉入站消息回调：幂等去重（msgId）→ 映射 → BotLoopGuard → 触发事件。
    /// 钉钉 webhook 回调无签名校验。
    /// </summary>
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

    private static string PeerKey(InboundMessage message)
    {
        if (message.Metadata is { } md && md.TryGetValue("peer", out var peer) && peer is not null)
        {
            return peer.ToString() ?? message.From;
        }
        return message.From;
    }
}
