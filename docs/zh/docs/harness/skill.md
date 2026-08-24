---
title: "技能"
description: "Markdown 技能仓库、SkillCatalog、技能加载工具与策展"
---

## 概述

技能（Skill）是可热加载的 **Markdown 提示词模块**：把成功模式写成 `.md` 文件放进技能仓库，智能体按需加载并使用。Core 与 Harness 两层提供不同粒度的设施：

| 层 | 类型 | 说明 |
|----|------|------|
| Core | `ISkill` / `ISkillRepository` / `MarkdownSkillParser` | 技能解析与仓库抽象 |
| Core | `SkillRegistry` / `SkillBox` / `SkillToolFactory` | 技能注册、按需加载、转为工具 |
| Core | `DynamicSkillMiddleware` / `SkillHook` | 运行时动态注入 |
| Harness | `WorkspaceSkillRepository` / `SkillCatalog` / `SkillLoadTool` | 工作区技能仓库与加载工具 |

## 技能文件格式

`MarkdownSkillParser` 解析 YAML front matter + Markdown 正文：

```
---
name: code-review
description: 对一段代码做严格的代码评审
version: 1.0.0
tags: [review, code]
---

对给定代码执行代码评审，重点检查：
1. 正确性与边界条件
2. 资源释放与并发安全
3. 命名与可读性
```

## 工作区技能仓库（Harness）

```csharp
using AgentScope.Harness.Skill;

// 扫描 .agentscope/skills/ 下所有 .md
var repo = new WorkspaceSkillRepository(".agentscope/workspace");   // 第二个参数默认 ".agentscope/skills"
IEnumerable<RegisteredSkill> skills = repo.Scan();
```

### SkillCatalog 与 SkillLoadTool

```csharp
using AgentScope.Harness.Skill.Runtime;

HarnessSkillEntry entry = ...;   // 技能条目
var catalog = SkillCatalog.Of(new[] { entry });
SkillLoadTool loadTool = new SkillLoadTool(catalog.All);   // 工具名 "load_skill"
```

`SkillLoadTool` 把按 id 加载技能内容的能力暴露给模型（`load_skill(skillId)`），技能正文进入上下文后由模型遵循执行。

### 运行时技能仓库

```csharp
using AgentScope.Harness.Skill;

// 根据 RuntimeContext.Current 选择底层仓库（例如多租户按租户选库）
var repo = new RuntimeContextSkillRepository(rc => new WorkspaceSkillRepository("workspace-" + (rc?.UserId ?? "default")));
```

## 技能工具组（Core）

`SkillToolGroup(name, tools, isActive = true)` 把一组技能工具组成组，经 `Toolkit.AddSkillGroup(...)` 注册后受组激活状态控制。`SkillToolFactory` 可将 `ISkill` 转换为 `ITool`。

## 技能策展（Curator）

Harness 提供技能生命周期管理（`AgentScope.Harness.Skill.Curator`）：

- `SkillUsageStore` + `SkillUsageMiddleware`（Order 760）：统计模型查看 / 使用技能次数；
- `SkillCurator` + `SkillCuratorMiddleware`（Order 780）：回合结束后台评估技能质量，`SkillPromoter` 把高频优质技能提升到默认可见；
- `SkillSecurityScanner`：发布前安全扫描；`SkillAuditLog` / `SkillUsageBackend` / `BaseStoreSkillUsageBackend`：审计与用量持久化。

```csharp
using AgentScope.Harness;
using AgentScope.Harness.Skill.Curator;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(model)
    .WithSkillUsageStore(new SkillUsageStore())        // 启用技能用量统计
    .WithSkillCurator(new SkillCurator())              // 启用技能策展
    .Build();
```

## 技能仓库扩展

| 扩展 | 类型 | 构造 |
|------|------|------|
| `AgentScope.Extensions.Skill.Git.GitSkillRepository` | `ISkillRepository` | `(string repoUrl, string branch = "main", string? localPath = null)` |
| `AgentScope.Extensions.Skill.MySql.MySqlSkillRepository` | `ISkillRepository` | `(string connectionString)` |
| `AgentScope.Extensions.Skill.PostgreSql.PostgreSqlSkillRepository` | `ISkillRepository` | `(string connectionString)` |
| `AgentScope.Extensions.Nacos.Skill.NacosSkillRepository` | 独立类 | `(string serverAddr, string? namespaceId = null, string? group = null, HttpClient? http = null)` |

## 相关文档

- [工作区](./workspace.md) —— `.agentscope/skills/` 目录约定
- [工具](../building-blocks/tool.md) —— Toolkit 与工具组
- [技能仓库集成](../../integration/skill/index.md)
