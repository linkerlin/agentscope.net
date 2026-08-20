# OSS Session State

Persist agent session state in Alibaba Cloud Object Storage Service (OSS) using the `AgentScope.Extensions.Store.Oss` package (powered by Aliyun.OSS SDK).

## Dependency

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.Oss" Version="2.0.1" />
</ItemGroup>
```

Target framework: net10.0.

## Quick Start

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.Model;
using AgentScope.Core.State;
using AgentScope.Extensions.Store.Oss;

var ossStore = new OssDistributedStore(
    httpClient,
    endpoint: "oss-cn-hangzhou.aliyuncs.com",
    bucket: "my-bucket",
    accessKeyId: "your-access-key",
    accessKeySecret: "your-access-secret");

var stateStore = new OssAgentStateStore(ossStore, keyPrefix: "agentstate");

var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);

EnhancedReActAgent agent = new EnhancedReActAgentBuilder()
    .Name("assistant")
    .Model(new DashScopeModel("qwen-plus", apiKey))
    .Memory(memory)
    .Build();

await agent.CallAsync(Msg.Builder().Role("user").TextContent("Hello").Build());
```

## Custom Key Prefix

```csharp
var stateStore = new OssAgentStateStore(ossStore, keyPrefix: "prod/session");
```

The default `keyPrefix` is `"agentstate"`.

## Save and Restore Session

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "oss-demo");

agent.SaveTo(session, "main");
agent.LoadIfExists(session, "main");
```

## Versioning Notes

OSS does **not** support versioning (`SupportsVersioning = false`); it uses last-writer-wins semantics. For multi-replica deployments, prefer a CAS-capable backend (Redis/MySQL/PostgreSQL).

## Production Considerations

- Use RAM Role + STS temporary credentials instead of hardcoded AK/SK.
- Configure bucket lifecycle rules to control storage costs.
- OSS is best suited for large-capacity snapshot scenarios; its latency is higher than Redis.
