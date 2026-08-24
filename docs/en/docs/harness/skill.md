---
title: "Skill"
description: "Markdown skill repository, SkillCatalog, skill loading tools, and curation"
---

## Overview

Skills are hot-loadable **Markdown prompt modules**: write successful patterns as `.md` files into the skill repository, and agents load and use them on demand. Core and Harness provide facilities at different granularities:

| Layer | Type | Description |
|----|------|------|
| Core | `ISkill` / `ISkillRepository` / `MarkdownSkillParser` | Skill parsing and repository abstraction |
| Core | `SkillRegistry` / `SkillBox` / `SkillToolFactory` | Skill registration, on-demand loading, conversion to tools |
| Core | `DynamicSkillMiddleware` / `SkillHook` | Runtime dynamic injection |
| Harness | `WorkspaceSkillRepository` / `SkillCatalog` / `SkillLoadTool` | Workspace skill repository and loading tools |

## Skill File Format

`MarkdownSkillParser` parses YAML front matter + Markdown body:

```
---
name: code-review
description: Perform a strict code review on a piece of code
version: 1.0.0
tags: [review, code]
---

Perform a code review on the given code, focusing on:
1. Correctness and boundary conditions
2. Resource release and concurrency safety
3. Naming and readability
```

## Workspace Skill Repository (Harness)

```csharp
using AgentScope.Harness.Skill;

// Scan all .md files under .agentscope/skills/
var repo = new WorkspaceSkillRepository(".agentscope/workspace");   // second parameter defaults to ".agentscope/skills"
IEnumerable<RegisteredSkill> skills = repo.Scan();
```

### SkillCatalog and SkillLoadTool

```csharp
using AgentScope.Harness.Skill.Runtime;

HarnessSkillEntry entry = ...;   // skill entry
var catalog = SkillCatalog.Of(new[] { entry });
SkillLoadTool loadTool = new SkillLoadTool(catalog.All);   // tool name "load_skill"
```

`SkillLoadTool` exposes the ability to load skill content by id to the model (`load_skill(skillId)`). The skill body enters the context and is followed by the model.

### Runtime Skill Repository

```csharp
using AgentScope.Harness.Skill;

// Select underlying repository based on RuntimeContext.Current (e.g., multi-tenant selection)
var repo = new RuntimeContextSkillRepository(rc => new WorkspaceSkillRepository("workspace-" + (rc?.UserId ?? "default")));
```

## Skill Tool Group (Core)

`SkillToolGroup(name, tools, isActive = true)` groups a set of skill tools. After registration via `Toolkit.AddSkillGroup(...)`, group activation state controls visibility. `SkillToolFactory` can convert an `ISkill` into an `ITool`.

## Skill Curation (Curator)

Harness provides skill lifecycle management (`AgentScope.Harness.Skill.Curator`):

- `SkillUsageStore` + `SkillUsageMiddleware` (Order 760): counts model view / use of skills;
- `SkillCurator` + `SkillCuratorMiddleware` (Order 780): evaluates skill quality in the background after turns, `SkillPromoter` promotes frequently used high-quality skills to default visibility;
- `SkillSecurityScanner`: security scan before publishing; `SkillAuditLog` / `SkillUsageBackend` / `BaseStoreSkillUsageBackend`: audit and usage persistence.

```csharp
using AgentScope.Harness;
using AgentScope.Harness.Skill.Curator;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithSkillUsageStore(new SkillUsageStore())        // enable skill usage stats
    .WithSkillCurator(new SkillCurator())              // enable skill curation
    .Build();
```

## Skill Repository Extensions

| Extension | Type | Constructor |
|------|------|------|
| `AgentScope.Extensions.Skill.Git.GitSkillRepository` | `ISkillRepository` | `(string repoUrl, string branch = "main", string? localPath = null)` |
| `AgentScope.Extensions.Skill.MySql.MySqlSkillRepository` | `ISkillRepository` | `(string connectionString)` |
| `AgentScope.Extensions.Skill.PostgreSql.PostgreSqlSkillRepository` | `ISkillRepository` | `(string connectionString)` |
| `AgentScope.Extensions.Nacos.Skill.NacosSkillRepository` | standalone class | `(string serverAddr, string? namespaceId = null, string? group = null, HttpClient? http = null)` |

## Related Documentation

- [Workspace](./workspace.md) —— `.agentscope/skills/` directory convention
- [Tool](../building-blocks/tool.md) —— Toolkit and tool groups
- [Skill Repository Integration](../../integration/skill/index.md)
