using AgentScope.Core.A2A.Client.Card;
using AgentScope.Core.A2A.Client.Utils;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Client;

/// <summary>
/// 面向远程 A2A Agent 的 AgentScope Agent 实现。对标 Java A2aAgent。
/// 通过 AgentCardResolver 解析远程 Agent 的 AgentCard，
/// 使用 A2A Client 发送消息、处理流式事件。
/// </summary>
public sealed class A2aAgent : AgentBase
{
    private readonly IAgentCardResolver _resolver;
    private readonly HttpClient _http;
    private readonly MessageConvertUtil _converter;
    private AgentCard? _card;

    public A2aAgent(string name, IAgentCardResolver resolver, HttpClient? http = null)
        : base(name, $"A2A Agent: {name}")
    {
        _resolver = resolver;
        _http = http ?? new HttpClient();
        _converter = new MessageConvertUtil();
    }

    protected override async Task<Msg> DoCallAsync(IReadOnlyList<Msg> messages)
    {
        _card ??= await _resolver.ResolveAsync(Name);
        if (_card == null) return Msg.Builder().Role("system").TextContent($"无法解析 AgentCard: {Name}").Build();

        var lastMsg = messages.Count > 0 ? messages[messages.Count - 1]
            : Msg.Builder().Role("user").TextContent("").Build();

        var parts = _converter.ConvertToParts(lastMsg);
        var payload = new
        {
            jsonrpc = "2.0",
            method = "tasks/send",
            @params = new
            {
                id = Guid.NewGuid().ToString(),
                sessionId = Guid.NewGuid().ToString(),
                message = new
                {
                    role = "user",
                    parts = parts,
                    metadata = new
                    {
                        _agentscope_msg_source = "agentscope-dotnet",
                        _agentscope_msg_id = Guid.NewGuid().ToString()
                    }
                }
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        using var resp = await _http.PostAsync(_card.Endpoint, content);
        resp.EnsureSuccessStatusCode();

        var responseJson = await resp.Content.ReadAsStringAsync();
        var doc = System.Text.Json.JsonDocument.Parse(responseJson);
        var result = doc.RootElement.TryGetProperty("result", out var r) ? r : doc.RootElement;

        if (result.TryGetProperty("message", out var msgEl))
        {
            var role = msgEl.TryGetProperty("role", out var roleEl) ? roleEl.GetString() : "assistant";
            var partsEl = msgEl.TryGetProperty("parts", out var p) ? p : default;
            var partList = new List<object>();
            if (partsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var part in partsEl.EnumerateArray())
                    partList.Add(part);
            }
            return _converter.ConvertFromParts(partList, role ?? "assistant");
        }

        return Msg.Builder().Role("assistant").TextContent(responseJson).Build();
    }
}
