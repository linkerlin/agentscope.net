# Git Skill Repository

`AgentScope.Extensions.Skill.Git` uses **LibGit2Sharp** to load skill definitions (`.skill.yaml` files) from a remote Git repository. Supports both HTTPS and SSH.

Package version: **2.0.1** | Target framework: **net10.0**

## When to use

- You want Git-based version control and review for skills
- You want to share skills across multiple projects

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Skill.Git" Version="2.0.1" />
</ItemGroup>
```

## Constructor

```csharp
public GitSkillRepository(
    string repoUrl,
    string branch = "main",
    string? localPath = null)
```

| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `repoUrl` | `string` | Yes | — | Remote repository URL |
| `branch` | `string` | No | `"main"` | Branch to check out |
| `localPath` | `string?` | No | Temp directory | Local clone path (null = auto temp dir) |

On construction, the repository clones (or opens an existing clone), scans all `*.skill.yaml` files, and caches them in memory.

## Public methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetSkillAsync(string, CancellationToken)` | `Task<Skill?>` | Gets a skill by name |
| `GetAllSkillNamesAsync(CancellationToken)` | `Task<IReadOnlyList<string>>` | Lists all skill names |
| `SkillExistsAsync(string, CancellationToken)` | `Task<bool>` | Checks if a skill exists |
| `Sync()` | `void` | Manually fetches remote updates and reloads the cache |
| `Dispose()` | `void` | Releases the LibGit2Sharp Repository |

## Sync

The initial clone happens at construction. Call `Sync()` to pull remote changes. The repository implements `IDisposable` — always dispose when done.
