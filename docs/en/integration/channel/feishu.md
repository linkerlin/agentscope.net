# Feishu Channel

`AgentScope.Extensions.Channel.Feishu` connects your Agent to Feishu / Lark (飞书) through webhook callbacks and the Feishu Open API.

Package version: **2.0.1** | Target framework: **net10.0**

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.Feishu" Version="2.0.1" />
</ItemGroup>
```

## Constructor

```csharp
public FeishuChannel(
    HttpClient http,
    string webhookUrl,
    string? appSecret = null,
    string? appId = null,
    string? encryptKey = null,
    string? verificationToken = null,
    string? apiBase = null)
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `http` | `HttpClient` | Yes | HTTP client for calling Feishu OpenAPI |
| `webhookUrl` | `string` | Yes | Webhook URL for outgoing messages |
| `appSecret` | `string?` | No | Feishu app secret |
| `appId` | `string?` | No | Feishu app id (e.g. `cli_xxxxx`) |
| `encryptKey` | `string?` | No | AES-256-CBC encrypt key (enables payload encryption) |
| `verificationToken` | `string?` | No | URL verification token |
| `apiBase` | `string?` | No | API base URL, default `https://open.feishu.cn` |

When both `appId` and `appSecret` are provided, `TokenProvider` exposes a `FeishuAccessTokenProvider`. When `encryptKey` is configured, `FeishuCrypto` is enabled.

## Interface members

| Member | Description |
|--------|-------------|
| `Name` | Returns `"feishu"` |
| `StartAsync` | No-op (stateless channel) |
| `StopAsync` | No-op |
| `SendAsync` | Sends text via webhook (`POST` to `webhookUrl`) |
| `ProcessInboundAsync` | Processes callback: optional signature verify (`X-Lark-Signature`) → decrypt → url_verification challenge → event_id dedup → mapping → BotLoopGuard → fires `OnMessageReceived` |
| `OnMessageReceived` | Inbound message event |

### URL verification handshake

When a `url_verification` challenge is detected, the channel auto-validates the `token` and returns a `{"challenge":"..."}` response.
