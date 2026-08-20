---
title: "Channel"
description: "Harness IChannel, ChannelRouter, and channel extensions"
---

## Overview

Channel (`AgentScope.Harness.Gateway.Channel`) connects external message channels (chat platforms, Web UI, bot webhooks) to the agent. A Channel is responsible for **inbound** (`DispatchAsync` handling messages) and **outbound** (`Deliver` delivering replies).

## IChannel

```csharp
public interface IChannel
{
    string ChannelId { get; }
    ChannelConfig Config { get; }
    void Init(IGateway gateway);                              // bind gateway
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task<Msg> DispatchAsync(InboundMessage message, CancellationToken ct = default);   // handle inbound
    void Deliver(OutboundAddress address, IReadOnlyList<Msg> messages);               // deliver outbound
}
```

### Core Types

```csharp
// Channel configuration
public sealed record ChannelConfig
{
    public string ChannelId { get; init; }
    public string DefaultAgentId { get; init; } = "";
    public DmScope DmScope { get; init; } = DmScope.Main;
    public IReadOnlyList<ChannelBinding> Bindings { get; init; } = [];
    public static ChannelConfig Of(string channelId);
    public static ChannelConfig Of(string channelId, string defaultAgentId);
}

// Inbound message
public sealed record InboundMessage
{
    public string ChannelId { get; init; }
    public string? AccountId { get; init; }
    public Peer Peer { get; init; }                 // Direct / Channel / Group / Thread
    public string? SenderId { get; init; }
    public string? Guild { get; init; }             // guild/organization
    public string? Team { get; init; }
    public IReadOnlySet<string> Roles { get; init; }
    public IReadOnlyList<Msg> Messages { get; init; }
    public string? PreferredAgentId { get; init; }
    public bool IsDm => Peer.Kind == PeerKind.Direct;
    public bool IsThread => Peer.Kind == PeerKind.Thread;
}

// Outbound address
public sealed record OutboundAddress
{
    public string ChannelId { get; init; }
    public string? AccountId { get; init; }
    public string To { get; init; }
    public string? ThreadId { get; init; }
    public static OutboundAddress Direct(string channelId, string to);
}

public enum PeerKind { Direct, Channel, Group, Thread }
public enum DmScope { Main, PerPeer, PerChannelPeer, PerAccountChannelPeer }
```

`Peer.Direct(id)` / `Peer.Channel(id)` / `Peer.Group(id)` / `Peer.Thread(id)` are static factories for Peer.

## ChannelRouter: Inbound Routing

`ChannelRouter(globalDefaultAgentId)` performs 8-level priority routing for inbound messages (`RouteResult(AgentId, MatchedBy, OutboundAddress)`): by `PreferredAgentId` → `ChannelBinding` (`ForPeer` / `ForGuild` / `ForTeam` / `ForAccount` / `ForChannel`, with optional `Roles` constraints) → `DmScope` session mapping → global default.

```csharp
var router = new ChannelRouter(globalDefaultAgentId: "main-agent");

var config = ChannelConfig.Of("wecom", defaultAgentId: "main-agent");
config.Bindings = new[]
{
    ChannelBinding.ForPeer("alice", agentId: "personal-agent"),
    ChannelBinding.ForGuild("dev-team", agentId: "team-agent")
};

RouteResult route = router.ResolveRoute(config, inboundMessage);
```

## ChannelFactory and Built-in ChatUiChannel

```csharp
using AgentScope.Harness.Gateway.Channel;

var factory = new ChannelFactory();
factory.Register("chatui", config => new ChatUiChannel(config));

IChannel channel = factory.Create("chatui", ChannelConfig.Of("chatui"));
```

`ChatUiChannel` (`ChannelIdConst = "chatui"`) is a built-in in-process channel: `Send(string text)` simulates user inbound, `PollOutbound()` fetches pending outbound messages (`OutboundEnvelope(Address, Messages, TimestampMs)`) — Web / TUI hosts can poll it directly.

## Implementing a Custom Channel

```csharp
public class MyChannel : IChannel
{
    public string ChannelId => "my-channel";
    public ChannelConfig Config { get; }
    private IGateway? _gateway;

    public MyChannel(ChannelConfig? config = null) => Config = config ?? ChannelConfig.Of("my-channel");

    public void Init(IGateway gateway) => _gateway = gateway;

    public async Task<Msg> DispatchAsync(InboundMessage message, CancellationToken ct = default)
    {
        // 1. Build session context (UserId/SessionId mapping is channel-specific)
        RuntimeContext rc = RuntimeContext.Empty
            .WithSessionId($"{ChannelId}:{message.Peer.Key}");
        // 2. Call Agent via gateway
        Msg reply = await _gateway!.RunAsync(message.Messages.Last(), rc, ct);
        // 3. Deliver reply
        Deliver(OutboundAddress.Direct(ChannelId, message.Peer.Id), new[] { reply });
        return reply;
    }

    public void Deliver(OutboundAddress address, IReadOnlyList<Msg> messages)
    {
        // Send to external platform
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
}
```

## Channel Extensions

`AgentScope.Extensions.Channel.*` provides instant messaging channel implementations (implementing the `AgentScope.Extensions.Channel.IChannel` interface in the umbrella project, parallel to Harness `IChannel`, requiring an adapter for integration):

| Extension | Core Type | Constructor |
|------|----------|------|
| `AgentScope.Extensions.Channel.DingTalk` | `DingTalkChannel` | `(HttpClient http, string webhookUrl, string? appSecret = null, string? appKey = null, string? apiBase = null)` |
| `AgentScope.Extensions.Channel.Feishu` | `FeishuChannel` | `(HttpClient http, string webhookUrl, string? appSecret = null, string? appId = null, string? encryptKey = null, string? verificationToken = null, string? apiBase = null)` |
| `AgentScope.Extensions.Channel.WeCom` | `WeComChannel` | `(HttpClient http, string webhookUrl, string? corpId = null, string? corpSecret = null, string? token = null, string? encodingAesKey = null, string? receiveId = null, string? apiBase = null)` |
| `AgentScope.Extensions.Channel.GitHub` | `GitHubChannel` | `(HttpClient http, string owner, string repo, string token, string? webhookSecret = null)` |
| `AgentScope.Extensions.Channel.GitLab` | `GitLabChannel` | `(HttpClient http, string gitlabUrl, string accessToken, string projectId, string? webhookToken = null)` |

See [Channel Integration](../../integration/channel/index.md) for details.

## Related Documentation

- [Harness Architecture](./architecture.md) —— Gateway and message bus
- [Message and Event](../building-blocks/message-and-event.md)
