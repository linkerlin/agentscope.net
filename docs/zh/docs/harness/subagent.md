---
title: "子 Agent"
description: "SubagentDeclaration、DefaultAgentManager 与动态生成"
---

## 概述

子 Agent（`AgentScope.Harness.Subagent`）让主 Agent 把任务委派给按需创建的独立智能体。声明、工厂、管理器三层分离：

### SubagentDeclaration

```csharp
public sealed record SubagentDeclaration(
    string Name,
    string Description,
    string? WorkspacePath = null,     // 关联工作区路径
    string? InlineBody = null,        // 内联 Markdown 规格正文
    string? RemoteUrl = null,         // 远端 Agent 地址（A2A/Agent Protocol）
    WorkspaceMode WorkspaceMode = WorkspaceMode.Shared)   // Shared（默认）/ Isolated
{
    public bool IsRemote => RemoteUrl != null;
}
```

### 从 Markdown 声明

`AgentSpecLoader` 解析 YAML front matter + Markdown 正文：

```
workspace/subagents/researcher.md
---
name: researcher
description: 检索资料并归纳要点
---
你是一名严谨的研究员。收到主题后先检索，再输出结构化摘要。
```

```csharp
SubagentDeclaration decl = AgentSpecLoader.Load("researcher");          // 从 subagents/ 目录按名加载
SubagentDeclaration parsed = AgentSpecLoader.Parse(markdown, "name");   // 直接解析字符串
```

### 工厂

```csharp
public delegate IAgent SubagentFactory(SubagentDeclaration declaration);
```

典型实现：根据声明构建一个带专属系统提示词（声明正文）的 `EnhancedReActAgent`。

### 管理器

```csharp
public interface ISubagentManager
{
    IAgent GetOrCreate(string specRef);   // 不存在则用工厂创建（specRef 通常为声明名）
    void Register(string name, IAgent agent);
    void Remove(string name);
}

// 默认实现：线程安全注册表
var manager = new DefaultAgentManager(factory);
```

## 与 Agent 集成

```csharp
using AgentScope.Harness;
using AgentScope.Harness.Subagent;

SubagentDeclaration declaration = AgentSpecLoader.Load("researcher");
ISubagentManager manager = new DefaultAgentManager(decl =>
{
    var sub = new EnhancedReActAgentBuilder()
        .Name(decl.Name)
        .SysPrompt(decl.InlineBody ?? decl.Description)
        .Model(subModel)
        .Build();
    return sub;
});

HarnessAgent agent = new HarnessAgentBuilder()
    .WithModel(mainModel)
    .WithSubagentManager(manager)     // 默认 DefaultAgentManager()（工厂为 null）
    .Build();
```

`SubagentsMiddleware`（Order 300，自动装配）把管理器注入 `ctx.Items["subagents"]`，供 `AgentSpawnTool` 等工具消费——模型通过 `agent_spawn` 类工具触发 `GetOrCreate` 并调用子 Agent。

## 远端子 Agent

声明带 `RemoteUrl` 时为远端子 Agent，调用经以下协议设施转发：

- `RemoteSubagentStub` / `RemoteAskPolicy`：本地存根与询问策略；
- `SubagentGatewayBridge`：与 Gateway 的桥接；
- `Tasks/` 子包：`BackgroundTask` / `TaskRepository` / `WorkspaceTaskRepository` / `AgentProtocolTaskClient` 等后台任务设施（提交、状态轮询、结果回投）。

## 团队（Team）

多个 Agent 的任务级协作走 `ITeamClient`（`AgentScope.Harness.Team`）：

```csharp
ITeamClient teams = new LocalTeamClient();   // 进程内实现，CAS 乐观并发

string taskId = await teams.CreateTaskAsync(new TeamTask(Id: "", Description: "调研竞品"));
bool claimed = await teams.ClaimTaskAsync(taskId, memberId: "agent-1");
await teams.CompleteTaskAsync(taskId, result: "...");
await foreach (TeamMessage msg in teams.ReadMessagesAsync(inbox: "agent-1")) { }
await teams.SendMessageAsync("agent-2", new TeamMessage("agent-1", "agent-2", "hi", DateTime.UtcNow));
```

`TeamsMiddleware`（Order 500，自动装配）注入 `ctx.Items["team"]`；`TeamTool` 把任务/消息操作暴露为模型工具。`TeamCreateSpec(Name, Description?, MemberIds?)` 用于创建团队。

## 相关文档

- [Harness 架构](./architecture.md)
- [Channel](./channel.md) —— 跨进程消息入口
