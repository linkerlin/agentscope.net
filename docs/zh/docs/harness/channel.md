---
title: "Channel"
description: "Harness IChannel、ChannelRouter 与渠道扩展"
---

## 概述

Channel（`AgentScope.Harness.Gateway.Channel`）把外部消息渠道（聊天平台、Web UI、机器人 webhook）接入智能体。一个 Channel 负责**入站**（`DispatchAsync` 处理消息）与**出站**（`Deliver` 投递回复）。

## IChannel

```csharp
public interface IChannel
{
    string ChannelId { get; }
    ChannelConfig Config { get; }
    void Init(IGateway gateway);                              // 绑定网关
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);
    Task<Msg> DispatchAsync(InboundMessage message, CancellationToken ct = default);   // 处理入站
    void Deliver(OutboundAddress address, IReadOnlyList<Msg> messages);               // 投递出站
}
```

### 核心类型

```csharp
// 渠道配置
public sealed record ChannelConfig
{
    public string ChannelId { get; init; }
    public string DefaultAgentId { get; init; } = "";
    public DmScope DmScope { get; init; } = DmScope.Main;
    public IReadOnlyList<ChannelBinding> Bindings { get; init; } = [];
    public static ChannelConfig Of(string channelId);
    public static ChannelConfig Of(string channelId, string defaultAgentId);
}

// 入站消息
public sealed record InboundMessage
{
    public string ChannelId { get; init; }
    public string? AccountId { get; init; }
    public Peer Peer { get; init; }                 // Direct / Channel / Group / Thread
    public string? SenderId { get; init; }
    public string? Guild { get; init; }             // 群/组织
    public string? Team { get; init; }
    public IReadOnlySet<string> Roles { get; init; }
    public IReadOnlyList<Msg> Messages { get; init; }
    public string? PreferredAgentId { get; init; }
    public bool IsDm => Peer.Kind == PeerKind.Direct;
    public bool IsThread => Peer.Kind == PeerKind.Thread;
}

// 出站地址
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

`Peer.Direct(id)` / `Peer.Channel(id)` / `Peer.Group(id)` / `Peer.Thread(id)` 是 Peer 的静态工厂。

## ChannelRouter：入站路由

`ChannelRouter(globalDefaultAgentId)` 对入站消息做 8 层优先级路由（`RouteResult(AgentId, MatchedBy, OutboundAddress)`）：按 `PreferredAgentId` → `ChannelBinding`（`ForPeer` / `ForGuild` / `ForTeam` / `ForAccount` / `ForChannel`，可带 `Roles` 约束）→ `DmScope` 会话映射 → 全局默认。

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

## ChannelFactory 与内置 ChatUiChannel

```csharp
using AgentScope.Harness.Gateway.Channel;

var factory = new ChannelFactory();
factory.Register("chatui", config => new ChatUiChannel(config));

IChannel channel = factory.Create("chatui", ChannelConfig.Of("chatui"));
```

`ChatUiChannel`（`ChannelIdConst = "chatui"`）是内置的进程内渠道：`Send(string text)` 模拟用户入站，`PollOutbound()` 拉取待投递的出站消息（`OutboundEnvelope(Address, Messages, TimestampMs)`）——Web / TUI 宿主可以直接轮询它。

## 实现一个自定义 Channel

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
        // 1. 构造会话上下文（UserId/SessionId 由渠道自行决定映射）
        RuntimeContext rc = RuntimeContext.Empty
            .WithSessionId($"{ChannelId}:{message.Peer.Key}");
        // 2. 通过网关调用 Agent
        Msg reply = await _gateway!.RunAsync(message.Messages.Last(), rc, ct);
        // 3. 投递回复
        Deliver(OutboundAddress.Direct(ChannelId, message.Peer.Id), new[] { reply });
        return reply;
    }

    public void Deliver(OutboundAddress address, IReadOnlyList<Msg> messages)
    {
        // 发送到外部平台
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;
}
```

## 渠道扩展

`AgentScope.Extensions.Channel.*` 提供即时通讯渠道实现（实现的是 `AgentScope.Extensions.Channel.IChannel` 接口，位于伞工程，与 Harness `IChannel` 平行，需适配器接入）：

| 扩展 | 核心类型 | 构造 |
|------|----------|------|
| `AgentScope.Extensions.Channel.DingTalk` | `DingTalkChannel` | `(HttpClient http, string webhookUrl, string? appSecret = null, string? appKey = null, string? apiBase = null)` |
| `AgentScope.Extensions.Channel.Feishu` | `FeishuChannel` | `(HttpClient http, string webhookUrl, string? appSecret = null, string? appId = null, string? encryptKey = null, string? verificationToken = null, string? apiBase = null)` |
| `AgentScope.Extensions.Channel.WeCom` | `WeComChannel` | `(HttpClient http, string webhookUrl, string? corpId = null, string? corpSecret = null, string? token = null, string? encodingAesKey = null, string? receiveId = null, string? apiBase = null)` |
| `AgentScope.Extensions.Channel.GitHub` | `GitHubChannel` | `(HttpClient http, string owner, string repo, string token, string? webhookSecret = null)` |
| `AgentScope.Extensions.Channel.GitLab` | `GitLabChannel` | `(HttpClient http, string gitlabUrl, string accessToken, string projectId, string? webhookToken = null)` |

详见 [渠道集成](../../integration/channel/index.md)。

## 相关文档

- [Harness 架构](./architecture.md) —— Gateway 与消息总线
- [消息与事件](../building-blocks/message-and-event.md)
