# WeCom Channel

`AgentScope.Extensions.Channel.WeCom` connects your Agent to WeCom (企业微信 / WeChat Work) via the **encrypted callback** mechanism. A controller receives message callbacks, decrypts them, and dispatches through the Gateway.

## When to use

- Your Agent needs to respond to WeCom bot messages in 1:1 chats or group chats.
- Your application already runs ASP.NET Core.

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.WeCom" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

## Prerequisites

1. Create an **Application** in the [WeCom Admin Console](https://work.weixin.qq.com/).
2. Enable the **Receive Messages** API and configure the callback URL:
   `https://your-host/api/channels/wecom/{channelId}/callback`
3. Note down the **Corp ID**, **Agent ID**, **Secret**, **Token**, and **EncodingAESKey**.

## Quickstart

```csharp
var channel = WeComChannel.FromProperties(
    "my-wecom",
    ChannelConfig.Of("my-wecom", "main"),
    new Dictionary<string, string>
    {
        ["corpId"] = "your-corp-id",
        ["agentId"] = "1000002",
        ["secret"] = "your-secret",
        ["token"] = "your-callback-token",
        ["encodingAesKey"] = "your-encoding-aes-key"
    });

var gw = GatewayBootstrap.Builder()
    .Agent("main", agent)
    .Channel(channel)
    .Build();

gw.Start();
```

## Configuration properties

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| `corpId` | Yes | — | Enterprise Corp ID |
| `agentId` | Yes | — | Application Agent ID |
| `secret` | Yes | — | Application secret for access token |
| `token` | Yes | — | Callback token for signature verification |
| `encodingAesKey` | Yes | — | AES key for message encryption/decryption |
| `callbackPath` | No | `/api/channels/wecom/{channelId}/callback` | Override the callback URL path |
| `apiBase` | No | `https://qyapi.weixin.qq.com` | WeCom API base URL |

## Encryption

All WeCom callbacks are encrypted. The adapter handles decryption and signature verification automatically using `WeComCrypto`, which implements the [WeCom callback encryption spec](https://developer.work.weixin.qq.com/document/path/90238).

## Message flow

**Inbound:** `WeComCallbackController` → URL verification (echostr) → decrypt → MsgId dedup → `WeComInboundMapper` (text messages) → bot-loop guard → Gateway.

**Outbound:** `WeComOutboundClient` sends replies via `/cgi-bin/message/send` (DMs) or `/cgi-bin/appchat/send` (groups), authenticating with an `access_token` from `WeComAccessTokenProvider`.
