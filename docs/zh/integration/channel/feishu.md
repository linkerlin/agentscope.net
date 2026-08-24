# 飞书 Channel

`AgentScope.Extensions.Channel.Feishu` 通过飞书开放平台 Webhook 和事件订阅回调机制将你的 Agent 接入飞书 / Lark。

包版本：**2.0.1** | 目标框架：**net10.0**

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.Feishu" Version="2.0.1" />
</ItemGroup>
```

## 构造函数

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

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `http` | `HttpClient` | 是 | 用于调用飞书 OpenAPI 的 HTTP 客户端 |
| `webhookUrl` | `string` | 是 | 出站消息的 Webhook 地址 |
| `appSecret` | `string?` | 否 | 飞书应用密钥 |
| `appId` | `string?` | 否 | 飞书应用 app_id（如 `cli_xxxxx`） |
| `encryptKey` | `string?` | 否 | AES-256-CBC 加密密钥（启用载荷加密） |
| `verificationToken` | `string?` | 否 | URL 校验验证令牌 |
| `apiBase` | `string?` | 否 | API 基地址，默认 `https://open.feishu.cn` |

当 `appId` 和 `appSecret` 均提供时，可通过 `TokenProvider` 获取 `FeishuAccessTokenProvider`。配置 `encryptKey` 后启用 `FeishuCrypto` 加解密。

## 接口实现

| 成员 | 说明 |
|------|------|
| `Name` | 返回 `"feishu"` |
| `StartAsync` | 无操作（无状态渠道） |
| `StopAsync` | 无操作 |
| `SendAsync` | 通过 Webhook 发送文本消息（`POST` 到 `webhookUrl`） |
| `ProcessInboundAsync` | 处理回调：可选验签（`X-Lark-Signature`）→ 解密 → url_verification 挑战 → event_id 去重 → mapping → BotLoopGuard → 触发 `OnMessageReceived` 事件 |
| `OnMessageReceived` | 入站消息事件 |

### URL 校验握手

当 `ProcessInboundAsync` 检测到 `url_verification` 挑战时，自动校验 `token` 并返回 `{"challenge":"..."}` 响应。
