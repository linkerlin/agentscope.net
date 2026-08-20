# WeCom Channel

`AgentScope.Extensions.Channel.WeCom` connects your Agent to WeCom (企业微信 / WeChat Work) through encrypted webhook callbacks.

Package version: **2.0.1** | Target framework: **net10.0**

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.WeCom" Version="2.0.1" />
</ItemGroup>
```

## Constructor

```csharp
public WeComChannel(
    HttpClient http,
    string webhookUrl,
    string? corpId = null,
    string? corpSecret = null,
    string? token = null,
    string? encodingAesKey = null,
    string? receiveId = null,
    string? apiBase = null)
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `http` | `HttpClient` | Yes | HTTP client for calling WeCom APIs |
| `webhookUrl` | `string` | Yes | Webhook URL for outgoing messages |
| `corpId` | `string?` | No | Enterprise Corp ID |
| `corpSecret` | `string?` | No | Application secret |
| `token` | `string?` | No | Callback token (for signature verification) |
| `encodingAesKey` | `string?` | No | AES key (for message encryption/decryption) |
| `receiveId` | `string?` | No | Receiver ID (corpId) |
| `apiBase` | `string?` | No | API base URL, default `https://qyapi.weixin.qq.com` |

When `corpId` and `corpSecret` are provided, `TokenProvider` exposes a `WeComAccessTokenProvider`. When `token`, `encodingAesKey`, and `receiveId` are all provided, `WeComCrypto` is enabled.

## Interface members

| Member | Description |
|--------|-------------|
| `Name` | Returns `"wecom"` |
| `StartAsync` | No-op (stateless channel) |
| `StopAsync` | No-op |
| `SendAsync` | Sends text via webhook (`POST` to `webhookUrl`) |
| `ProcessInboundAsync` | Processes callback: `msg_signature` verify → decrypt → URL verification (echostr) → MsgId dedup → mapping → BotLoopGuard → fires `OnMessageReceived` |
| `OnMessageReceived` | Inbound message event |

All WeCom callbacks are encrypted. Without `token`/`encodingAesKey`/`receiveId`, `ProcessInboundAsync` returns `FailedVerification`.
