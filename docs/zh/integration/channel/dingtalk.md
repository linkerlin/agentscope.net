# 钉钉 Channel

`AgentScope.Extensions.Channel.DingTalk` 通过钉钉 Webhook 和回调机制将你的 Agent 接入钉钉。

包版本：**2.0.1** | 目标框架：**net10.0**

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.DingTalk" Version="2.0.1" />
</ItemGroup>
```

## 构造函数

```csharp
public DingTalkChannel(
    HttpClient http,
    string webhookUrl,
    string? appSecret = null,
    string? appKey = null,
    string? apiBase = null)
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `http` | `HttpClient` | 是 | 用于调用钉钉 API 的 HTTP 客户端 |
| `webhookUrl` | `string` | 是 | 出站消息的 Webhook 地址 |
| `appSecret` | `string?` | 否 | 钉钉应用密钥（用于 token 型 API 调用） |
| `appKey` | `string?` | 否 | 钉钉应用 Key |
| `apiBase` | `string?` | 否 | 自定义 API 基地址，默认 `https://api.dingtalk.com` |

当 `appKey` 和 `appSecret` 均提供时，可通过 `TokenProvider` 属性获取 `DingTalkAccessTokenProvider`。

## 接口实现

| 成员 | 说明 |
|------|------|
| `Name` | 返回 `"dingtalk"` |
| `StartAsync` | 无操作（无状态渠道） |
| `StopAsync` | 无操作 |
| `SendAsync` | 通过 Webhook 发送文本消息（`POST` 到 `webhookUrl`） |
| `ProcessInboundAsync` | 处理回调：JSON 解析 → msgId 去重 → mapping → BotLoopGuard → 触发 `OnMessageReceived` 事件 |
| `OnMessageReceived` | 入站消息事件 |

入站回调无签名校验（钉钉 Webhook 回调格式）。`ProcessInboundAsync` 返回 `InboundProcessResult` 指示验证和派发结果。
