# Nacos

`AgentScope.Extensions.Nacos` 将 [Nacos](https://nacos.io/) 作为 AgentScope 的统一控制面：注册发现 A2A Agent、动态加载 Prompt、托管 Skill。包含三个子模块，按需组合。

| 子模块 | 解决的问题 |
| --- | --- |
| `AgentScope.Extensions.Nacos`（核心） | A2A AgentCard 注册发现，实现 `IAgentRegistry` |
| `AgentScope.Extensions.Nacos.Prompt` | 把 Prompt 模板存入 Nacos，运行时热更新 |
| `AgentScope.Extensions.Nacos.Skill` | 从 Nacos 加载 Skill 包 |

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Nacos" Version="2.0.1" />
```

## A2A 注册发现

### NacosAgentRegistry（服务端注册）

`NacosAgentRegistry` 实现 `AgentScope.Core.Service.Discovery.IAgentRegistry` 接口，通过 Nacos HTTP Open API 注册/注销/发现 Agent。

```csharp
using AgentScope.Extensions.Nacos;
using AgentScope.Core.Service.Discovery;

var registry = new NacosAgentRegistry(
    httpClient,
    serverAddr: "http://localhost:8848",
    groupName: "DEFAULT_GROUP");

// 注册 AgentCard
await registry.RegisterAsync(new AgentCard(
    "agent-1", "translator", "翻译 Agent", "192.168.1.1:8080"));

// 通过名称解析
var card = await registry.ResolveAsync("translator");

// 列举所有已注册 Agent
await foreach (var c in registry.ListAsync()) { ... }

// 注销
await registry.UnregisterAsync("translator");
```

### IAgentRegistry 接口

| 方法 | 说明 |
| --- | --- |
| `ValueTask RegisterAsync(AgentCard card, CancellationToken ct)` | 注册 AgentCard 为 Nacos 临时实例 |
| `ValueTask UnregisterAsync(string agentId, CancellationToken ct)` | 从 Nacos 删除实例 |
| `ValueTask<AgentCard?> ResolveAsync(string agentId, CancellationToken ct)` | 查询健康实例并构建 AgentCard |
| `IAsyncEnumerable<AgentCard> ListAsync(CancellationToken ct)` | 列举所有已注册的 AgentCard |

### NacosAgentCardResolver（客户端发现）

```csharp
var resolver = new NacosAgentCardResolver(
    httpClient,
    serverAddr: "http://localhost:8848",
    groupName: "DEFAULT_GROUP");

var card = await resolver.ResolveAsync("translator");
```

### 配置选项

通过 `NacosA2aRegistryOptions` 可注入配置：

```csharp
var options = new NacosA2aRegistryOptions
{
    ServerAddr = "http://localhost:8848",
    Namespace = "",
    GroupName = "DEFAULT_GROUP",
    HeartbeatInterval = TimeSpan.FromSeconds(5)
};
```

## Prompt 配置中心

通过 `AgentScope.Extensions.Nacos.Prompt` 引入：

```csharp
using AgentScope.Extensions.Nacos.Prompt;

var repo = new NacosPromptRepository(
    serverAddr: "http://localhost:8848",
    namespaceId: null,
    group: null,
    http: httpClient);
```

`NacosPromptRepository` 从 Nacos 配置中心读取 Prompt 模板，支持运行时热更新。

## Skill 仓库

通过 `AgentScope.Extensions.Nacos.Skill` 引入：

```csharp
using AgentScope.Extensions.Nacos.Skill;

var repo = new NacosSkillRepository(
    serverAddr: "http://localhost:8848",
    namespaceId: null,
    group: null,
    http: httpClient);
```

`NacosSkillRepository` 从 Nacos 加载技能包，实现 `AgentScope.Extensions.Skill.ISkillRepository` 接口。

## 与其他扩展配合

- 结合 [A2A](../protocol/a2a.md)：`AgentScopeA2aServer.AddRegistry(registry)` 可在 A2A Server 启动时将 AgentCard 注册到 Nacos。
- 结合 [A2A 客户端](../protocol/a2a.md)：`NacosAgentCardResolver` 可作为 `A2aAgent` 的 `IAgentCardResolver` 实现。
