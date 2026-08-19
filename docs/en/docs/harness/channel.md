---
title: "Channel"
description: "Route messages, manage sessions, and stream events through Channel"
---

## What they do

**Gateway** sits between your application code and the agent. It handles:

- **Session management** — maps each user conversation to a stable session id. The agent sees consistent memory across turns.
- **Per-session concurrency control** — concurrent messages to the same session are queued fairly so the agent never races itself.
- **Agent routing** — in multi-agent setups, routes each message to the right agent.

**Channel** adapts a messaging platform (HTTP, WebSocket, Slack, etc.) into the Gateway's routing model. It resolves who sent the message, which agent should handle it, and where to deliver the reply.

For most use cases you don't interact with Gateway or Channel directly — `agent.channel(...)` wires everything up behind the scenes.

## Quick start

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("assistant")
    .SysPrompt("You are a helpful assistant.")
    .Model("dashscope:qwen-plus")
    .Build();

// Bind a ChatUI channel.
ChatUiChannel chat = agent.Channel(ChatUiChannel.Create());

// Send messages. Each userId gets its own session automatically.
Msg reply = chat.Send(SendOptions.UserId("user-1"), "Hello!").GetAwaiter().GetResult();

// Same user, same session — conversation continues.
Msg followUp = chat.Send(SendOptions.UserId("user-1"), "Tell me more.").GetAwaiter().GetResult();

// Different user, different session.
Msg otherUser = chat.Send(SendOptions.UserId("user-2"), "Hi there").GetAwaiter().GetResult();
```

`agent.channel(...)` lazily creates an internal gateway, registers the agent, and injects the gateway into the channel. After this call, `chat` is ready to use.

### SendOptions

`SendOptions` tells the channel **who** is talking and **which conversation** this belongs to:

| Factory | Behavior |
|---------|----------|
| `SendOptions.UserId("user-1")` | One session per user (most common) |
| `SendOptions.Of("user-1", "session-a")` | Explicit session — multiple conversations per user |
| `SendOptions.UserId("user-1").WithAgentId("support")` | Route to a specific agent in multi-agent setups |
| `SendOptions.UserId("user-1").WithAttribute("tenant", "acme")` | Attach string/typed attributes to the agent turn |
| `SendOptions.UserId("user-1").WithRuntimeContext(rtc)` | Carry a full `RuntimeContext` (e.g. force-sync flags) |

```csharp
// Same user, two independent conversations
chat.Send(SendOptions.Of("user-1", "session-a"), "Topic A").GetAwaiter().GetResult();
chat.Send(SendOptions.Of("user-1", "session-b"), "Topic B").GetAwaiter().GetResult();

// Pass application context into the agent turn
chat.Send(
        SendOptions.UserId("user-1")
                .WithAttribute("tenant", "acme")
                .WithRuntimeContext(
                        RuntimeContext.Builder()
                                .Put(AgentSpawnTool.CTX_FORCE_SYNC, true)
                                .Build()),
        "Investigate the ticket")
        .GetAwaiter().GetResult();
```

### Multimodal / structured messages

Plain-text `Send(String)` is a convenience. For images, audio, or multi-part turns, pass a pre-built `Msg` (or `List<Msg>`) — every String overload has matching `Msg` / `List<Msg>` variants (including `SendOptions` and `sendStream`):

```csharp
Msg multimodal = Msg.Builder()
        .Role(MsgRole.USER)
        .Content(
                TextBlock.Builder().Text("What is in this image?").Build(),
                ImageBlock.Builder()
                        .Source(URLSource.Builder().Url("https://example.com/photo.png").Build())
                        .Build())
        .Build();

chat.Send(SendOptions.UserId("user-1"), multimodal).GetAwaiter().GetResult();
chat.Send(SendOptions.UserId("user-1"), new List<Msg> { multimodal }).GetAwaiter().GetResult();
chat.Send(multimodal).GetAwaiter().GetResult(); // single-session mode
```

### RuntimeContext merge

Channel turns always build a `RuntimeContext` inside the Gateway. Callers can contribute a **caller base** via `SendOptions` / `InboundMessage.RuntimeContext()` / a `ChannelRuntimeContextResolver`. Merge order:

1. Start from the caller context (may be empty)
2. If a `ChannelRuntimeContextResolver` is configured and returns non-null, that value **replaces** the caller base
3. Gateway overlays identity fields — `sessionId` (`gw-…`), `userId`, `msgContext`, `gateKey`, `outboundAddress` — which always win on conflict

Wire a resolver through `GatewayBootstrap`:

```csharp
GatewayBootstrap gw = GatewayBootstrap.Builder()
        .Agent("main", b => b.Name("assistant").Model(model))
        .RuntimeContextResolver(req =>
                RuntimeContext.Builder(req.CallerContext())
                        .Put("tenant", ResolveTenant(req))
                        .Build())
        .Build();
ChatUiChannel chat = gw.ChatUiChannel();
```

Or call `gateway.SetRuntimeContextResolver(...)` after obtaining the gateway. Do **not** put business attributes in `MsgContext.Extra` — that map participates in the session key.

## Streaming events + SSE

`SendStream()` returns `IAsyncEnumerable<AgentEvent>` — the same fine-grained event stream as `agent.StreamEvents()`, but routed through the gateway with session management.

```csharp
await foreach (var event in chat.SendStream(SendOptions.UserId("user-1"), "What is the weather in Beijing?"))
{
    if (event is TextBlockDeltaEvent delta)
    {
        Console.Write(delta.Delta);
    }
    else if (event is ToolCallStartEvent tc)
    {
        Console.WriteLine($"\n[tool] {tc.ToolCallName}");
    }
}
```

### ASP.NET Core SSE controller

```csharp
[HttpGet("/chat")]
public async IAsyncEnumerable<string> Chat([FromQuery] string message,
                                           [FromQuery] string userId,
                                           [FromQuery] string? sessionId)
{
    SendOptions options = sessionId != null
            ? SendOptions.Of(userId, sessionId)
            : SendOptions.UserId(userId);

    await foreach (var event in chat.SendStream(options, message))
    {
        var payload = new Dictionary<string, object?>
        {
            ["type"] = event.Type.ToString(),
            ["id"] = event.Id
        };
        if (event is TextBlockDeltaEvent delta)
        {
            payload["delta"] = delta.Delta;
        }
        else if (event is SubagentExposedEvent se)
        {
            payload["subagentId"] = se.SubagentId;
            payload["agentId"] = se.AgentId;
            payload["label"] = se.Label;
        }
        yield return System.Text.Json.JsonSerializer.Serialize(payload);
    }
}
```

## Talking to exposed subagents

When the agent spawns a subagent with `expose_to_user=true`, the gateway exposes that subagent as a user-addressable entry point. A `SubagentExposedEvent` is emitted into the `SendStream()` event stream carrying the `subagentId`.

### Discovering exposed subagents

```csharp
string? subagentId = null;

await foreach (var event in chat.SendStream(SendOptions.UserId("user-1"), "Spawn a researcher to investigate AI trends"))
{
    if (event is SubagentExposedEvent se)
    {
        subagentId = se.SubagentId;
        Console.WriteLine($"Subagent exposed: id={se.SubagentId} agent={se.AgentId} label={se.Label}");
    }
    if (event is TextBlockDeltaEvent delta)
    {
        Console.Write(delta.Delta);
    }
}
```

`SubagentExposedEvent` fields:

| Field | Description |
|-------|-------------|
| `subagentId` | Handle for sending messages to this subagent |
| `agentId` | Subagent type (e.g. `"researcher"`) |
| `sessionId` | Subagent's session id |
| `label` | Optional human-readable name |

### Sending messages to subagents

Once you have a `subagentId`, send messages directly to the subagent — bypassing the parent agent entirely:

```csharp
// Non-streaming
Msg reply = chat.SendToSubagent(subagentId, "Focus on LLM agents").GetAwaiter().GetResult();

// Streaming
await foreach (var event in chat.SendToSubagentStream(subagentId, "Focus on LLM agents"))
{
    if (event is TextBlockDeltaEvent delta)
    {
        Console.Write(delta.Delta);
    }
}
```

### SSE with subagent support

A typical SSE controller handles both main-agent and subagent messages:

```csharp
[HttpGet("/chat")]
public async IAsyncEnumerable<string> Chat([FromQuery] string userId,
                                           [FromQuery] string message,
                                           [FromQuery] string? subagentId)
{
    IAsyncEnumerable<AgentEvent> events;
    if (subagentId != null)
    {
        events = chat.SendToSubagentStream(subagentId, message);
    }
    else
    {
        events = chat.SendStream(SendOptions.UserId(userId), message);
    }
    await foreach (var event in events)
    {
        yield return ToSSE(event);
    }
}
```

The client watches for `SUBAGENT_EXPOSED` events to render new conversation tabs, and passes the `subagentId` back on subsequent requests.

## Multi-agent routing

For scenarios with multiple `HarnessAgent` instances, use `GatewayBootstrap`:

```csharp
HarnessAgent salesAgent = HarnessAgent.Builder()
    .Name("sales").SysPrompt("You are a sales assistant.")
    .Model("dashscope:qwen-plus").Build();

HarnessAgent supportAgent = HarnessAgent.Builder()
    .Name("support").SysPrompt("You are a support agent.")
    .Model("dashscope:qwen-plus").Build();

GatewayBootstrap gw = GatewayBootstrap.Builder()
    .Agent("sales", salesAgent)
    .Agent("support", supportAgent)
    .MainAgent("sales")          // default when no agent is specified
    .Build();

ChatUiChannel chat = gw.ChatUiChannel();
```

### Routing by agentId

Use `SendOptions.WithAgentId()` to route a message to a specific agent:

```csharp
// Routes to sales (the default main agent)
chat.Send(SendOptions.UserId("user-1"), "What products?").GetAwaiter().GetResult();

// Routes to support explicitly
chat.Send(SendOptions.UserId("user-1").WithAgentId("support"), "Billing issue").GetAwaiter().GetResult();
```

### Thread exposure with GatewayBootstrap

To enable `expose_to_user` on subagents, wire the gateway bridge into each agent's subagent middleware:

```csharp
GatewayBootstrap gw = GatewayBootstrap.Builder()
    .Agent("main", mainAgent)
    .Build();

// Wire the bridge so agent_spawn(expose_to_user=true) works.
SubagentGatewayBridge bridge = gw.GatewayBridge();
// Pass bridge to the agent's SubagentsMiddleware via SetGatewayBridge().
```

With `agent.channel(...)`, this wiring happens automatically.

## Custom Channel

Implement the `IChannel` interface to adapt a new messaging platform:

```csharp
public class MySlackChannel : IChannel
{
    public string ChannelId() => "slack";
    public ChannelConfig Config() => myConfig;
    public void Init(Gateway gateway) => this.gateway = gateway;
    public void Start() => /* connect to Slack */;
    public void Stop() => /* disconnect */;

    public Task<Msg> Dispatch(InboundMessage message)
    {
        RouteResult route = router.ResolveRoute(Config(), message);
        return gateway.Run(route.Context(), message.Messages(), route.OutboundAddress());
    }

    // Optional: streaming dispatch
    public IAsyncEnumerable<AgentEvent> DispatchStream(InboundMessage message)
    {
        RouteResult route = router.ResolveRoute(Config(), message);
        return gateway.RunStream(route.Context(), message.Messages(), route.OutboundAddress());
    }
}
```

Register it with `GatewayBootstrap`:

```csharp
GatewayBootstrap gw = GatewayBootstrap.Builder()
    .Agent("main", agent)
    .Channel(new MySlackChannel())
    .Build();

gw.Start();   // calls Init() + Start() on all channels
// ...
gw.Stop();    // calls Stop() on all channels
```

## Built-in channel adapters

AgentScope provides ready-to-use Channel adapters for popular messaging platforms as extension modules:

- [DingTalk](../../integration/channel/dingtalk.md) — Stream protocol (persistent WebSocket)
- [Feishu / Lark](../../integration/channel/feishu.md) — Event subscription callback
- [GitHub](../../integration/channel/github.md) — Issue / PR comment webhook
- [GitLab](../../integration/channel/gitlab.md) — Note hook
- [WeCom](../../integration/channel/wecom.md) — Encrypted callback

See the [Channel Adapters](../../integration/channel/index.md) integration overview for details.

## Related pages

- [Subagent](./subagent.md) — declaring and spawning subagents, background tasks, streaming forwarding
- [Architecture](./architecture.md) — how parent and child agents cooperate
