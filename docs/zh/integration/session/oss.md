# OSS 会话状态

使用 `AgentScope.Extensions.Store.Oss` 包（基于 Aliyun.OSS SDK）将 Agent 会话状态持久化到阿里云对象存储。

## 安装

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.Oss" Version="2.0.1" />
</ItemGroup>
```

目标框架：net10.0。

## 快速开始

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

await agent.CallAsync(Msg.Builder().Role("user").TextContent("你好").Build());
```

## 自定义 key 前缀

```csharp
var stateStore = new OssAgentStateStore(ossStore, keyPrefix: "prod/session");
```

默认 `keyPrefix` 为 `"agentstate"`。

## 会话保存与恢复

```csharp
using AgentScope.Core.Session;

var sessionManager = new SessionManager();
Session session = sessionManager.CreateSession(name: "oss-demo");

agent.SaveTo(session, "main");
agent.LoadIfExists(session, "main");
```

## 版本化说明

OSS 后端**不支持**版本化（`SupportsVersioning = false`），使用 last-writer-wins 语义。多副本场景建议使用支持 CAS 的后端（Redis/MySQL/PostgreSQL）。

## 生产建议

- 使用 RAM Role + STS 临时凭证替代硬编码 AK/SK。
- 配置 Bucket 生命周期规则控制存储成本。
- OSS 适合大容量快照场景，但延迟高于 Redis。
