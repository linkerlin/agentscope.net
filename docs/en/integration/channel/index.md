# Channel Extensions

These extensions connect your Agent to external messaging platforms through the `AgentScope.Extensions.Channel.IChannel` interface. Each channel class handles platform-specific authentication, signature verification, message parsing, and reply delivery.

> **Note:** `AgentScope.Extensions.Channel.IChannel` (event callback + `SendAsync`, webhook-client style) is different from `AgentScope.Harness.Gateway.Channel.IChannel` (`DispatchAsync`/`Deliver`, gateway-routing style). Extension channels must be adapted for Harness integration.

| Extension | Namespace | Transport |
| --- | --- | --- |
| [DingTalk](dingtalk.md) | `AgentScope.Extensions.Channel.DingTalk` | Webhook callback (HTTP) |
| [Feishu](feishu.md) | `AgentScope.Extensions.Channel.Feishu` | Event subscription callback (HTTP) |
| [WeCom](wecom.md) | `AgentScope.Extensions.Channel.WeCom` | Encrypted callback (HTTP) |
| [GitHub](github.md) | `AgentScope.Extensions.Channel.GitHub` | Webhook (HTTP) |
| [GitLab](gitlab.md) | `AgentScope.Extensions.Channel.GitLab` | Webhook (HTTP) |

## How it works

1. **Inbound** — `ProcessInboundAsync` receives platform callbacks (raw body + headers), performs signature verification, message deduplication, BotLoopGuard, then fires the `OnMessageReceived` event.
2. **Outbound** — `SendAsync` takes a `Msg` object and delivers it via the platform's API or webhook URL.

All adapters share two common utilities from `AgentScope.Extensions.Channel.Common`:

- **IdempotencyStore** — deduplicates retried webhook deliveries by message id.
- **BotLoopGuard** — per-peer rate limiter preventing bot-to-bot loops.

## Shared dependency

Every channel package depends on `AgentScope.Extensions.Channel.Common` (transitive) and `AgentScope.Extensions.Channel` (the umbrella project defining `IChannel`). The host provides `HttpClient` at runtime.
