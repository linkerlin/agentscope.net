# Channel 扩展

这些扩展通过 `AgentScope.Extensions.Channel.IChannel` 接口将你的 Agent 接入外部消息平台。每个渠道类负责平台特有的认证、签名校验、消息解析和回复投递。

> **注意区分**：`AgentScope.Extensions.Channel.IChannel`（事件回调 + `SendAsync`，webhook 客户端风格）与 `AgentScope.Harness.Gateway.Channel.IChannel`（`DispatchAsync`/`Deliver`，网关路由风格）是两个不同的接口。扩展渠道需通过适配层接入 Harness。

| 扩展 | 命名空间 | 传输方式 |
| --- | --- | --- |
| [钉钉](dingtalk.md) | `AgentScope.Extensions.Channel.DingTalk` | Webhook 回调（HTTP） |
| [飞书](feishu.md) | `AgentScope.Extensions.Channel.Feishu` | 事件订阅回调（HTTP） |
| [企业微信](wecom.md) | `AgentScope.Extensions.Channel.WeCom` | 加密回调（HTTP） |
| [GitHub](github.md) | `AgentScope.Extensions.Channel.GitHub` | Webhook（HTTP） |
| [GitLab](gitlab.md) | `AgentScope.Extensions.Channel.GitLab` | Webhook（HTTP） |

## 工作原理

所有渠道实现遵循相同模式：

1. **入站** — `ProcessInboundAsync` 接收平台回调（原始请求体 + 请求头），依次执行签名校验、消息去重、BotLoopGuard 防循环保护，然后通过 `OnMessageReceived` 事件触发分发。
2. **出站** — `SendAsync` 接收 `Msg` 对象，通过平台 API 或 webhook URL 发送出去。

所有适配器共享 `AgentScope.Extensions.Channel.Common` 中的两个通用组件：

- **IdempotencyStore** — 按消息 id 去重，防止 webhook 重试导致重复处理。
- **BotLoopGuard** — 按 peer 限速，防止 bot 之间的消息死循环。

## 共享依赖

每个 channel 扩展包依赖 `AgentScope.Extensions.Channel.Common`（传递依赖自动引入）和 `AgentScope.Extensions.Channel`（`IChannel` 接口定义所在伞工程），运行时由宿主提供 `HttpClient`。
