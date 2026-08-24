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
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using AgentScope.Core.Message;
using AgentScope.Extensions.Channel;
using AgentScope.Extensions.Channel.Common;

namespace AgentScope.Extensions.Channel.GitHub;

/// <summary>
/// GitHub Issue/PR 消息渠道。对标 Java GitHubChannel。
/// 通过 GitHub API 创建 Issue 或 PR 评论，并处理 webhook 投递（入站）。
/// </summary>
public sealed class GitHubChannel : IChannel
{
    private readonly HttpClient _http;
    private readonly string _owner;
    private readonly string _repo;
    private readonly string _token;
    private readonly GitHubSignatureVerifier? _signatureVerifier;
    private readonly GitHubInboundMapper _mapper;
    private readonly IdempotencyStore _idempotency = new();
    private readonly BotLoopGuard _botLoopGuard = new();

    public string Name => "github";
    public event Func<InboundMessage, Task>? OnMessageReceived;

    public GitHubChannel(
        HttpClient http,
        string owner,
        string repo,
        string token,
        string? webhookSecret = null)
    {
        _http = http;
        _owner = owner;
        _repo = repo;
        _token = token;
        if (!string.IsNullOrWhiteSpace(webhookSecret))
        {
            _signatureVerifier = new GitHubSignatureVerifier(webhookSecret);
        }
        _mapper = new GitHubInboundMapper(Name);
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

    public async ValueTask SendAsync(Msg message, CancellationToken ct = default)
    {
        var payload = new GitHubIssueBody { body = message.GetTextContent() ?? "" };
        using var req = new HttpRequestMessage(HttpMethod.Post,
            $"https://api.github.com/repos/{_owner}/{_repo}/issues")
        {
            Content = JsonContent.Create(payload)
        };
        req.Headers.UserAgent.ParseAdd("AgentScope.NET");
        req.Headers.Authorization = new("Bearer", _token);
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 处理 GitHub webhook 投递：验签（X-Hub-Signature-256）→ 事件过滤 → 幂等去重（comment.id）→ 映射 → BotLoopGuard → 触发事件。
    /// </summary>
    public async ValueTask<InboundProcessResult> ProcessInboundAsync(
        string rawBody, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        // 1. 验签（强制）
        if (_signatureVerifier is null)
        {
            return InboundProcessResult.FailedVerification;
        }
        var signature256 = Header(headers, "X-Hub-Signature-256");
        if (!_signatureVerifier.Verify(signature256, Encoding.UTF8.GetBytes(rawBody)))
        {
            return InboundProcessResult.FailedVerification;
        }

        // 2. 事件类型过滤（MVP 仅处理两种评论事件）
        var eventType = Header(headers, "X-GitHub-Event");
        if (!string.Equals(eventType, "issue_comment", StringComparison.Ordinal)
            && !string.Equals(eventType, "pull_request_review_comment", StringComparison.Ordinal))
        {
            return InboundProcessResult.Dispatched([]);
        }

        // 3. 解析
        JsonNode? payload;
        try
        {
            payload = JsonNode.Parse(rawBody);
        }
        catch (JsonException)
        {
            return InboundProcessResult.Dispatched([]);
        }

        // 4. 幂等去重（comment.id）
        var commentId = GitHubInboundMapper.ExtractCommentId(payload);
        if (commentId is not null && !_idempotency.FirstSeen($"{Name}|{commentId.Value}"))
        {
            return InboundProcessResult.SkippedAsDuplicate;
        }

        // 5. 映射
        var inbound = _mapper.Map(eventType, payload);
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

    private sealed record GitHubIssueBody(string title = "Agent Message", string body = "");
}
