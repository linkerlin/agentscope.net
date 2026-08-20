# GitHub Channel

`AgentScope.Extensions.Channel.GitHub` 将你的 Agent 接入 GitHub issue 和 PR 评论线程。通过 GitHub Webhook 接收评论回调，通过 REST API 创建回复。

包版本：**2.0.1** | 目标框架：**net10.0**

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.GitHub" Version="2.0.1" />
</ItemGroup>
```

## 构造函数

```csharp
public GitHubChannel(
    HttpClient http,
    string owner,
    string repo,
    string token,
    string? webhookSecret = null)
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `http` | `HttpClient` | 是 | 用于调用 GitHub REST API 的 HTTP 客户端 |
| `owner` | `string` | 是 | 仓库所属用户/组织 |
| `repo` | `string` | 是 | 仓库名称 |
| `token` | `string` | 是 | Personal Access Token（PAT） |
| `webhookSecret` | `string?` | 否 | Webhook 共享密钥（用于 `X-Hub-Signature-256` 验签） |

提供 `webhookSecret` 时启用 `GitHubSignatureVerifier` 签名校验。

## 接口实现

| 成员 | 说明 |
|------|------|
| `Name` | 返回 `"github"` |
| `StartAsync` | 无操作（无状态渠道） |
| `StopAsync` | 无操作 |
| `SendAsync` | 创建新 Issue（`POST /repos/{owner}/{repo}/issues`），附带 Bearer Token 认证 |
| `ProcessInboundAsync` | 处理 webhook：`X-Hub-Signature-256` 验签 → 事件过滤（`issue_comment`/`pull_request_review_comment`）→ comment.id 去重 → mapping（仅 `action=created`）→ BotLoopGuard → 触发 `OnMessageReceived` 事件 |
| `OnMessageReceived` | 入站消息事件 |
