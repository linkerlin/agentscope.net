# Feishu Channel

`AgentScope.Extensions.Channel.Feishu` connects your Agent to Feishu / Lark (飞书) via the **Event Subscription v2** callback mechanism. A controller receives webhook callbacks, optionally decrypts encrypted payloads, and dispatches messages through the Gateway.

## When to use

- Your Agent needs to respond to Feishu bot messages in 1:1 chats or group @-mentions.
- Your application already runs ASP.NET Core (the callback controller auto-registers).

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.Feishu" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

## Prerequisites

1. Create a **Custom App** in the [Feishu Developer Console](https://open.feishu.cn/).
2. Enable the **Bot** capability.
3. Configure the **Event Subscription** callback URL to point to your application:
   `https://your-host/api/channels/feishu/{channelId}/callback`
4. Note down the **App ID** and **App Secret**. Optionally configure an **Encrypt Key** and **Verification Token**.

## Quickstart

```csharp
var channel = FeishuChannel.FromProperties(
    "my-feishu",
    ChannelConfig.Of("my-feishu", "main"),
    new Dictionary<string, string>
    {
        ["appId"] = "cli_xxxxx",
        ["appSecret"] = "your-app-secret"
    });

var gw = GatewayBootstrap.Builder()
    .Agent("main", agent)
    .Channel(channel)
    .Build();

gw.Start();
```

The `FeishuCallbackController` is a controller that auto-registers at `/api/channels/feishu/{channelId}/callback`. It handles the URL verification handshake automatically.

## Configuration properties

| Property | Required | Default | Description |
|----------|----------|---------|-------------|
| `appId` | Yes | — | Feishu custom-app id (cli_xxx) |
| `appSecret` | Yes | — | Feishu custom-app secret |
| `encryptKey` | No | — | AES-256-CBC encrypt key; enables payload encryption |
| `verificationToken` | No | — | URL verification token for the challenge handshake |
| `callbackPath` | No | `/api/channels/feishu/{channelId}/callback` | Override the callback URL path |
| `apiBase` | No | `https://open.feishu.cn` | Feishu Open API base URL |

## Encryption

When `encryptKey` is configured, the callback body arrives as `{"encrypt":"<base64>"}`. The adapter decrypts it automatically (AES-256-CBC with SHA-256 key derivation) and verifies the `X-Lark-Signature` header.

## Message flow

**Inbound:** `FeishuCallbackController` → optional decryption → URL verification check → event_id dedup → `FeishuInboundMapper` (text messages only in MVP) → bot-loop guard → Gateway.

**Outbound:** `FeishuOutboundClient` sends replies via `POST /open-apis/im/v1/messages` with a `tenant_access_token` from `FeishuAccessTokenProvider`. Tokens are cached and proactively refreshed at ~80% of TTL.
