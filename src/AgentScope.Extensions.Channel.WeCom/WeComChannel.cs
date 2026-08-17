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
using System.Xml.Linq;
using AgentScope.Core.Message;
using AgentScope.Extensions.Channel;
using AgentScope.Extensions.Channel.Common;

namespace AgentScope.Extensions.Channel.WeCom;

/// <summary>
/// 企业微信消息渠道。对标 Java WeComChannel。
/// 通过企业微信 Webhook 发送消息，并处理加密回调（入站）。
/// </summary>
public sealed class WeComChannel : IChannel
{
    private readonly HttpClient _http;
    private readonly string _webhookUrl;
    private readonly WeComAccessTokenProvider? _tokenProvider;
    private readonly WeComCrypto? _crypto;
    private readonly WeComInboundMapper _mapper;
    private readonly IdempotencyStore _idempotency = new();
    private readonly BotLoopGuard _botLoopGuard = new();

    public string Name => "wecom";
    public event Func<InboundMessage, Task>? OnMessageReceived;

    public WeComChannel(
        HttpClient http,
        string webhookUrl,
        string? corpId = null,
        string? corpSecret = null,
        string? token = null,
        string? encodingAesKey = null,
        string? receiveId = null,
        string? apiBase = null)
    {
        _http = http;
        _webhookUrl = webhookUrl;
        if (corpId is not null && corpSecret is not null)
        {
            _tokenProvider = new WeComAccessTokenProvider(
                http, apiBase ?? "https://qyapi.weixin.qq.com", corpId, corpSecret);
        }
        if (token is not null && encodingAesKey is not null && receiveId is not null)
        {
            _crypto = new WeComCrypto(token, encodingAesKey, receiveId);
        }
        _mapper = new WeComInboundMapper(Name, corpId ?? "");
    }

    /// <summary>AccessToken 提供者（配置了 corpId/corpSecret 时可用）。</summary>
    public WeComAccessTokenProvider? TokenProvider => _tokenProvider;

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
        using var resp = await _http.PostAsync(_webhookUrl, json, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 处理企业微信回调：验签（msg_signature）→ 解密 → 幂等去重（MsgId）→ 映射 → BotLoopGuard → 触发事件。
    /// 同时支持 URL 校验握手（headers 携带 echostr 时解密返回）。
    /// </summary>
    public async ValueTask<InboundProcessResult> ProcessInboundAsync(
        string rawBody, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        if (_crypto is null)
        {
            // 未配置 token/encodingAesKey/receiveId，无法验签解密。
            return InboundProcessResult.FailedVerification;
        }

        var signature = Header(headers, "msg_signature");
        var timestamp = Header(headers, "timestamp");
        var nonce = Header(headers, "nonce");

        // URL 校验握手：echo 解密后的 echostr。
        var echostr = Header(headers, "echostr");
        if (echostr is not null)
        {
            if (!_crypto.VerifySignature(signature, timestamp, nonce, echostr))
            {
                return InboundProcessResult.FailedVerification;
            }
            try
            {
                return new InboundProcessResult
                {
                    Supported = true,
                    Verified = true,
                    Messages = [],
                    ChallengeResponse = _crypto.Decrypt(echostr),
                };
            }
            catch (InvalidOperationException)
            {
                return InboundProcessResult.FailedVerification;
            }
        }

        // 1. 提取 <Encrypt>
        var encrypt = WeComInboundMapper.ExtractEncrypt(rawBody);
        if (encrypt is null)
        {
            return InboundProcessResult.FailedVerification;
        }

        // 2. 验签
        if (!_crypto.VerifySignature(signature, timestamp, nonce, encrypt))
        {
            return InboundProcessResult.FailedVerification;
        }

        // 3. 解密
        string xml;
        try
        {
            xml = _crypto.Decrypt(encrypt);
        }
        catch (InvalidOperationException)
        {
            return InboundProcessResult.FailedVerification;
        }

        // 4. 幂等去重（MsgId）
        var msgId = WeComInboundMapper.ExtractMsgId(xml);
        if (msgId is not null && !_idempotency.FirstSeen($"{Name}|{msgId}"))
        {
            return InboundProcessResult.SkippedAsDuplicate;
        }

        // 5. 映射
        InboundMessage? inbound;
        try
        {
            inbound = _mapper.Map(xml);
        }
        catch (InvalidOperationException)
        {
            return InboundProcessResult.Dispatched([]);
        }
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
}
