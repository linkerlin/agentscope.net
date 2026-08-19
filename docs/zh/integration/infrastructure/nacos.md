# Nacos

`AgentScope.Extensions.Nacos` 把 [Nacos](https://nacos.io/) 用作 AgentScope 的统一控制面：注册发现 A2A Agent、动态加载 Prompt、托管 Skill。包含三个子模块，按需组合使用。

| 子模块 | 解决的问题 |
| --- | --- |
| `AgentScope.Extensions.Nacos.A2A` | A2A AgentCard 与服务实例的注册/发现 |
| `AgentScope.Extensions.Nacos.Prompt` | 把 Prompt 模板放到 Nacos，热更新到运行中的 Agent |
| `AgentScope.Extensions.Nacos.Skill` | 从 Nacos AI 模块加载 Skill 包（ZIP） |

## A2A 注册发现

### 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Nacos.A2A" Version="$(AgentScopeVersion)" />
```

### 服务端：把 AgentCard 注册到 Nacos

```csharp
using AgentScope.Core.Nacos.A2A.Registry;

Properties props = new();
props.SetProperty("serverAddr", "127.0.0.1:8848");
NacosA2aRegistry registry = new(props);

NacosA2aRegistryProperties props2 = new();
// props2.SetNamespace(...) / SetGroup(...) 等
registry.RegisterAgent(agentCard, props2);
```

注册后，AgentCard 与服务端点会写入 Nacos 的 AI Service，供消费者发现。

### 客户端：通过 Nacos 拿到远端 AgentCard

```csharp
using AgentScope.Core.Nacos.A2A.Discovery;

NacosAgentCardResolver resolver = new(props, "translator-agent");
A2aAgent remote = A2aAgent.Builder()
    .Name("translator")
    .AgentCardResolver(resolver)
    .Build();
```

## Prompt 配置中心

### 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Nacos.Prompt" Version="$(AgentScopeVersion)" />
```

### 用法

```csharp
using AgentScope.Core.Nacos.Prompt;

NacosPromptListener prompts = new(aiService);

string tpl = prompts.GetPrompt("system-prompt", new Dictionary<string, string>
{
    ["userName"] = "Alice"
});
```

监听器内部维护本地缓存，Nacos 上 prompt 更新时会自动推送进来，下一次 `GetPrompt(...)` 立即拿到新版本，无需重启。

## Skill 仓库

`AgentScope.Extensions.Nacos.Skill` 提供一个 `AgentSkillRepository` 实现，把 Nacos AI 模块管理的技能 ZIP 包下载下来解析。

```xml
<PackageReference Include="AgentScope.Extensions.Nacos.Skill" Version="$(AgentScopeVersion)" />
```

```csharp
using AgentScope.Core.Nacos.Skill;

Properties props = new();
props.SetProperty(NacosSkillRepository.SKILL_VERSION_PATH, "1.2.0");
// 或 SKILL_LABEL_PATH = "stable"

NacosSkillRepository repo = new(aiService, "default-namespace", props);
AgentSkill skill = repo.GetSkill("calculator");
```

版本/标签的解析顺序是：构造时传入的 `Properties` → 系统属性 → 环境变量。同时设置版本和标签时，**版本优先**，标签不会用于下载。

## 与其他扩展配合

- 结合 [A2A](../protocol/a2a.md)：`AgentScopeA2aServer.Builder().AgentRegistry(...)` 可以注入一个把 AgentCard 推到 Nacos 的注册器，启动后自动暴露给整个集群。
- 结合 [Skill 仓库](../skill/)：可以与 Git/MySQL 的 `AgentSkillRepository` 并存，把同一个 Toolkit 用多个数据源拼起来。
