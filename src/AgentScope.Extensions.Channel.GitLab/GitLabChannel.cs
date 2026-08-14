using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using AgentScope.Core.Message;
using AgentScope.Extensions.Channel;
using AgentScope.Extensions.Channel.Common;

namespace AgentScope.Extensions.Channel.GitLab;

public sealed class GitLabChannel : IChannel
{
    private readonly HttpClient _http;
    private readonly string _gitlabUrl;
    private readonly string _accessToken;
    private readonly string _projectId;
    private readonly string? _webhookToken;
    private readonly GitLabInboundMapper _mapper;
    private readonly IdempotencyStore _idempotency = new();
    private readonly BotLoopGuard _botLoopGuard = new();

    public string Name => "gitlab";
    public event Func<InboundMessage, Task>? OnMessageReceived;

    public GitLabChannel(
        HttpClient http,
        string gitlabUrl,
        string accessToken,
        string projectId,
        string? webhookToken = null)
    {
        _http = http;
        _gitlabUrl = gitlabUrl.TrimEnd('/');
        _accessToken = accessToken;
        _projectId = projectId;
        _webhookToken = webhookToken;
        _mapper = new GitLabInboundMapper(Name);
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

    public async ValueTask SendAsync(Msg message, CancellationToken ct = default)
    {
        var text = message.GetTextContent() ?? "";
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_gitlabUrl}/api/v4/projects/{_projectId}/issues");
        req.Headers.Add("PRIVATE-TOKEN", _accessToken);
        req.Content = JsonContent.Create(new { title = "Agent Message", description = text });
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// 处理 GitLab webhook 投递：X-Gitlab-Token 校验 → 事件过滤（Note Hook）→ 幂等去重（note.id）→ 映射 → BotLoopGuard → 触发事件。
    /// </summary>
    public async ValueTask<InboundProcessResult> ProcessInboundAsync(
        string rawBody, IReadOnlyDictionary<string, string>? headers = null, CancellationToken ct = default)
    {
        // 1. Token 校验（常量时间相等）
        if (string.IsNullOrWhiteSpace(_webhookToken))
        {
            return InboundProcessResult.FailedVerification;
        }
        var token = Header(headers, "X-Gitlab-Token");
        if (!ConstantTimeEquals(_webhookToken, token))
        {
            return InboundProcessResult.FailedVerification;
        }

        // 2. 事件类型过滤（MVP 仅 Note Hook）
        var eventType = Header(headers, "X-Gitlab-Event");
        if (!string.Equals(eventType, "Note Hook", StringComparison.Ordinal))
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

        // 4. 幂等去重（note id）
        var noteId = GitLabInboundMapper.ExtractNoteId(payload);
        if (noteId is not null && !_idempotency.FirstSeen($"{Name}|{noteId.Value}"))
        {
            return InboundProcessResult.SkippedAsDuplicate;
        }

        // 5. 映射
        var inbound = _mapper.Map(payload);
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

    private static bool ConstantTimeEquals(string? a, string? b)
    {
        if (a is null || b is null || a.Length != b.Length)
        {
            return false;
        }
        int r = 0;
        for (int i = 0; i < a.Length; i++)
        {
            r |= a[i] ^ b[i];
        }
        return r == 0;
    }
}
