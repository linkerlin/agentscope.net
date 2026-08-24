# DingTalk Channel

`AgentScope.Extensions.Channel.DingTalk` connects your Agent to DingTalk (钉钉) through webhook callbacks and the DingTalk OpenAPI.

Package version: **2.0.1** | Target framework: **net10.0**

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.DingTalk" Version="2.0.1" />
</ItemGroup>
```

## Constructor

```csharp
public DingTalkChannel(
    HttpClient http,
    string webhookUrl,
    string? appSecret = null,
    string? appKey = null,
    string? apiBase = null)
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `http` | `HttpClient` | Yes | HTTP client for calling DingTalk APIs |
| `webhookUrl` | `string` | Yes | Webhook URL for outgoing messages |
| `appSecret` | `string?` | No | DingTalk app secret (for token-based API calls) |
| `appKey` | `string?` | No | DingTalk app key |
| `apiBase` | `string?` | No | Custom API base URL, default `https://api.dingtalk.com` |

When both `appKey` and `appSecret` are provided, `TokenProvider` exposes a `DingTalkAccessTokenProvider`.

## Interface members

| Member | Description |
|--------|-------------|
| `Name` | Returns `"dingtalk"` |
| `StartAsync` | No-op (stateless channel) |
| `StopAsync` | No-op |
| `SendAsync` | Sends text via webhook (`POST` to `webhookUrl`) |
| `ProcessInboundAsync` | Processes callback: JSON parse → msgId dedup → mapping → BotLoopGuard → fires `OnMessageReceived` |
| `OnMessageReceived` | Inbound message event |

Inbound callbacks have no signature verification (DingTalk webhook callback format).
