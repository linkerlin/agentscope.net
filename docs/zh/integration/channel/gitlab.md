# GitLab Channel

`AgentScope.Extensions.Channel.GitLab` 将你的 Agent 接入 GitLab 评论（Note）hook。通过 GitLab Webhook 接收评论回调，通过 REST API 创建 Issue。

包版本：**2.0.1** | 目标框架：**net10.0**

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.GitLab" Version="2.0.1" />
</ItemGroup>
```

## 构造函数

```csharp
public GitLabChannel(
    HttpClient http,
    string gitlabUrl,
    string accessToken,
    string projectId,
    string? webhookToken = null)
```

| 参数 | 类型 | 必填 | 说明 |
|------|------|------|------|
| `http` | `HttpClient` | 是 | 用于调用 GitLab API 的 HTTP 客户端 |
| `gitlabUrl` | `string` | 是 | GitLab 实例 URL（如 `https://gitlab.com`） |
| `accessToken` | `string` | 是 | GitLab API 访问令牌 |
| `projectId` | `string` | 是 | 项目 ID |
| `webhookToken` | `string?` | 否 | Webhook Secret Token（用于 `X-Gitlab-Token` 校验） |

提供 `webhookToken` 时启用常量时间 token 校验。

## 接口实现

| 成员 | 说明 |
|------|------|
| `Name` | 返回 `"gitlab"` |
| `StartAsync` | 无操作（无状态渠道） |
| `StopAsync` | 无操作 |
| `SendAsync` | 创建新 Issue（`POST /api/v4/projects/{projectId}/issues`），附带 `PRIVATE-TOKEN` 认证 |
| `ProcessInboundAsync` | 处理 webhook：`X-Gitlab-Token` 常量时间校验 → 事件过滤（仅 `Note Hook`）→ note.id 去重 → mapping → BotLoopGuard → 触发 `OnMessageReceived` 事件 |
| `OnMessageReceived` | 入站消息事件 |
