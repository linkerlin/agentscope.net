# GitHub Channel

`AgentScope.Extensions.Channel.GitHub` connects your Agent to GitHub issue and PR comment threads. It receives callbacks via webhook and replies via the REST API.

Package version: **2.0.1** | Target framework: **net10.0**

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Channel.GitHub" Version="2.0.1" />
</ItemGroup>
```

## Constructor

```csharp
public GitHubChannel(
    HttpClient http,
    string owner,
    string repo,
    string token,
    string? webhookSecret = null)
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `http` | `HttpClient` | Yes | HTTP client for calling GitHub REST API |
| `owner` | `string` | Yes | Repository owner (user or org) |
| `repo` | `string` | Yes | Repository name |
| `token` | `string` | Yes | Personal Access Token (PAT) |
| `webhookSecret` | `string?` | No | Webhook shared secret for `X-Hub-Signature-256` verification |

`GitHubSignatureVerifier` is enabled when `webhookSecret` is provided.

## Interface members

| Member | Description |
|--------|-------------|
| `Name` | Returns `"github"` |
| `StartAsync` | No-op (stateless channel) |
| `StopAsync` | No-op |
| `SendAsync` | Creates a new Issue (`POST /repos/{owner}/{repo}/issues`) with Bearer token auth |
| `ProcessInboundAsync` | Processes webhook: `X-Hub-Signature-256` verify → event filter (`issue_comment`/`pull_request_review_comment`) → comment.id dedup → mapping (only `action=created`) → BotLoopGuard → fires `OnMessageReceived` |
| `OnMessageReceived` | Inbound message event |
