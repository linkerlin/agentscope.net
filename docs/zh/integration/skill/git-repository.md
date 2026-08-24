# Git 技能仓库

`AgentScope.Extensions.Skill.Git` 使用 **LibGit2Sharp** 从远程 Git 仓库加载技能定义（`.skill.yaml` 文件），支持 HTTPS 和 SSH。

包版本：**2.0.1** | 目标框架：**net10.0**

## 何时使用

- 想用 Git 管控技能版本与审阅
- 跨多个项目共享同一份技能集

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Skill.Git" Version="2.0.1" />
</ItemGroup>
```

## 构造函数

```csharp
public GitSkillRepository(
    string repoUrl,
    string branch = "main",
    string? localPath = null)
```

| 参数 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `repoUrl` | `string` | 是 | — | 远程仓库 URL |
| `branch` | `string` | 否 | `"main"` | 检出的分支 |
| `localPath` | `string?` | 否 | 临时目录 | 本地 clone 路径（null 自动创建临时目录） |

仓库初始化时自动 clone 或打开已有仓库，扫描 `*.skill.yaml` 文件并缓存到内存。

## 公开方法

| 方法 | 返回 | 说明 |
|------|------|------|
| `GetSkillAsync(string, CancellationToken)` | `Task<Skill?>` | 按名称获取技能 |
| `GetAllSkillNamesAsync(CancellationToken)` | `Task<IReadOnlyList<string>>` | 获取所有技能名称 |
| `SkillExistsAsync(string, CancellationToken)` | `Task<bool>` | 检查技能是否存在 |
| `Sync()` | `void` | 手动拉取远端更新并重新加载缓存 |
| `Dispose()` | `void` | 释放 LibGit2Sharp Repository 资源 |

## 同步

首次构造时自动 clone 并加载。`Sync()` 方法手动拉取远端更新。仓库实现了 `IDisposable`，使用完毕后应释放。
