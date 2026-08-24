# 企业微信 Channel

`AgentScope.Extensions.Channel.WeCom` 通过企业微信 Webhook 和加密回调机制将你的 Agent 接入企业微信（WeCom / WeChat Work）。

包版本：**2.0.1** | 目标框架：**net10.0**

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.WeCom" Version="2.0.1" />
</ItemGroup>
```

## 构造函数

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

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `http` | `HttpClient` | 是 | 用于调用企业微信 API 的 HTTP 客户端 |
| `webhookUrl` | `string` | 是 | 出站消息的 Webhook 地址 |
| `corpId` | `string?` | 否 | 企业 Corp ID |
| `corpSecret` | `string?` | 否 | 应用密钥 |
| `token` | `string?` | 否 | 回调 token（用于签名校验） |
| `encodingAesKey` | `string?` | 否 | AES 密钥（用于消息加解密） |
| `receiveId` | `string?` | 否 | 接收方 ID（corpId） |
| `apiBase` | `string?` | 否 | API 基地址，默认 `https://qyapi.weixin.qq.com` |

当 `corpId` 和 `corpSecret` 均提供时，可通过 `TokenProvider` 获取 `WeComAccessTokenProvider`。
当 `token`、`encodingAesKey` 和 `receiveId` 均提供时启用 `WeComCrypto` 加解密。

## 接口实现

| 成员 | 说明 |
|------|------|
| `Name` | 返回 `"wecom"` |
| `StartAsync` | 无操作（无状态渠道） |
| `StopAsync` | 无操作 |
| `SendAsync` | 通过 Webhook 发送文本消息（`POST` 到 `webhookUrl`） |
| `ProcessInboundAsync` | 处理回调：验签（`msg_signature`）→ 解密 → URL 校验（echostr）→ MsgId 去重 → mapping → BotLoopGuard → 触发 `OnMessageReceived` 事件 |
| `OnMessageReceived` | 入站消息事件 |

企业微信所有回调均强制加密。未配置 `token`/`encodingAesKey`/`receiveId` 时 `ProcessInboundAsync` 返回 `FailedVerification`。
