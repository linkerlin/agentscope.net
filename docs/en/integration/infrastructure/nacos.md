# Nacos

`AgentScope.Extensions.Nacos` uses [Nacos](https://nacos.io/) as AgentScope's unified control plane: register and discover A2A Agents, hot-load prompts, and host skills. It contains three sub-modules — pick the ones you need.

| Sub-module | Problem it solves |
| --- | --- |
| `AgentScope.Extensions.Nacos.A2A` | A2A AgentCard / instance registry and discovery |
| `AgentScope.Extensions.Nacos.Prompt` | Manage prompt templates in Nacos with hot updates |
| `AgentScope.Extensions.Nacos.Skill` | Load skill packages (ZIP) from the Nacos AI module |

## A2A registry & discovery

### Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Nacos.A2A" Version="$(AgentScopeVersion)" />
```

### Server side: register the AgentCard with Nacos

```csharp
using AgentScope.Core.Nacos.A2A.Registry;

Properties props = new();
props.SetProperty("serverAddr", "127.0.0.1:8848");
NacosA2aRegistry registry = new(props);

NacosA2aRegistryProperties props2 = new();
// props2.SetNamespace(...) / SetGroup(...) / etc.
registry.RegisterAgent(agentCard, props2);
```

After registration, the AgentCard and the service endpoint are written into the Nacos AI Service for consumers to discover.

### Client side: resolve a remote AgentCard via Nacos

```csharp
using AgentScope.Core.Nacos.A2A.Discovery;

NacosAgentCardResolver resolver = new(props, "translator-agent");
A2aAgent remote = A2aAgent.Builder()
    .Name("translator")
    .AgentCardResolver(resolver)
    .Build();
```

## Prompt config center

### Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Nacos.Prompt" Version="$(AgentScopeVersion)" />
```

### Usage

```csharp
using AgentScope.Core.Nacos.Prompt;

NacosPromptListener prompts = new(aiService);

string tpl = prompts.GetPrompt("system-prompt", new Dictionary<string, string>
{
    ["userName"] = "Alice"
});
```

The listener maintains a local cache; when prompts change in Nacos, updates are pushed in. The next `GetPrompt(...)` call returns the new version with no restart.

## Skill repository

`AgentScope.Extensions.Nacos.Skill` provides an `AgentSkillRepository` implementation that downloads and parses skill ZIP packages managed by the Nacos AI module.

```xml
<PackageReference Include="AgentScope.Extensions.Nacos.Skill" Version="$(AgentScopeVersion)" />
```

```csharp
using AgentScope.Core.Nacos.Skill;

Properties props = new();
props.SetProperty(NacosSkillRepository.SKILL_VERSION_PATH, "1.2.0");
// or SKILL_LABEL_PATH = "stable"

NacosSkillRepository repo = new(aiService, "default-namespace", props);
AgentSkill skill = repo.GetSkill("calculator");
```

Version/label resolution order: `Properties` provided to the constructor → system properties → environment variables. When both version and label resolve, **version wins** and the label is not used for download.

## Pairs well with

- [A2A](../protocol/a2a.md): inject a Nacos-backed `AgentRegistry` into `AgentScopeA2aServer.Builder().AgentRegistry(...)` to publish AgentCards cluster-wide on startup.
- [Skill repositories](../skill/): coexist with Git/MySQL `AgentSkillRepository` to assemble a Toolkit from multiple sources.
