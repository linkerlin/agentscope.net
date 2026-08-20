# GitLab Channel

`AgentScope.Extensions.Channel.GitLab` connects your Agent to GitLab note (comment) hooks. It receives callbacks via webhook and replies via the REST API.

Package version: **2.0.1** | Target framework: **net10.0**

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.GitLab" Version="2.0.1" />
</ItemGroup>
```

## Constructor

```csharp
public GitLabChannel(
    HttpClient http,
    string gitlabUrl,
    string accessToken,
    string projectId,
    string? webhookToken = null)
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `http` | `HttpClient` | Yes | HTTP client for calling GitLab API |
| `gitlabUrl` | `string` | Yes | GitLab instance URL (e.g. `https://gitlab.com`) |
| `accessToken` | `string` | Yes | GitLab API access token |
| `projectId` | `string` | Yes | Project ID |
| `webhookToken` | `string?` | No | Webhook secret token (for `X-Gitlab-Token` verification) |

Constant-time token comparison is used when `webhookToken` is provided.

## Interface members

| Member | Description |
|--------|-------------|
| `Name` | Returns `"gitlab"` |
| `StartAsync` | No-op (stateless channel) |
| `StopAsync` | No-op |
| `SendAsync` | Creates a new Issue (`POST /api/v4/projects/{projectId}/issues`) with `PRIVATE-TOKEN` auth |
| `ProcessInboundAsync` | Processes webhook: `X-Gitlab-Token` constant-time verify → event filter (only `Note Hook`) → note.id dedup → mapping → BotLoopGuard → fires `OnMessageReceived` |
| `OnMessageReceived` | Inbound message event |
