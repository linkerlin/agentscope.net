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
    private readonly HttpClient _http;
    private readonly string _webhookUrl;
    private readonly string? _appSecret;
    private readonly string? _appId;
    private readonly FeishuAccessTokenProvider? _tokenProvider;
    private readonly FeishuCrypto? _crypto;
    private readonly string? _verificationToken;
    private readonly FeishuInboundMapper _mapper;
    private readonly IdempotencyStore _idempotency = new();
    private readonly BotLoopGuard _botLoopGuard = new();

    public string Name => "feishu";
    public event Func<InboundMessage, Task>? OnMessageReceived;

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

    /// <summary>AccessToken 提供者（配置了 appId/appSecret 时可用）。</summary>
    public FeishuAccessTokenProvider? TokenProvider => _tokenProvider;

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

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
    /// 处理飞书回调：可选验签 → 解密 → url_verification 挑战 → 幂等去重（event_id）→ 映射 → BotLoopGuard → 触发事件。
    /// </summary>
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

    private static string PeerKey(InboundMessage message)
    {
        if (message.Metadata is { } md && md.TryGetValue("peer", out var peer) && peer is not null)
        {
            return peer.ToString() ?? message.From;
        }
        return message.From;
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
}
