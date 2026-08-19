# Alibaba Cloud OSS

`AgentScope.Extensions.Oss` provides distributed storage backed by Alibaba Cloud Object Storage Service (OSS), ideal for large-capacity data and Alibaba Cloud ecosystems.

## Dependency

```xml
<PackageReference Include="AgentScope.Extensions.Oss" Version="$(AgentScopeVersion)" />
```

## One-Line Setup

```csharp
using AgentScope.Extensions.Oss;

OssClient ossClient = new OssClient(endpoint, accessKeyId, accessKeySecret);
DistributedStore store = OssDistributedStore.Create(ossClient, "my-bucket", "agentscope/");

HarnessAgent agent = HarnessAgent.Builder()
    .DistributedStore(store)
    .Filesystem(new RemoteFilesystemSpec()
            .IsolationScope(IsolationScope.USER))
    .Build();
```

## Components Provided

### 1. OssAgentStateStore

Agent state persisted to OSS objects.

### 2. OssBaseStore

Workspace filesystem KV storage to OSS objects.

### 3. OssSnapshotSpec

Sandbox snapshots to OSS — the best choice for large workspace archives.

### Not Provided: SandboxExecutionGuard

Object storage is unsuitable for distributed locking. Mix in a Redis guard:

```csharp
DistributedStore ossStore = OssDistributedStore.Create(ossClient, "my-bucket", "agentscope/");

DistributedStore mixed = DistributedStore.Builder()
    .AgentStateStore(ossStore.AgentStateStore())
    .BaseStore(ossStore.BaseStore())
    .SandboxSnapshotSpec(ossStore.SandboxSnapshotSpec())
    .SandboxExecutionGuard(RedisDistributedStore.FromJedis(jedis).SandboxExecutionGuard())
    .Build();
```

## When to Use

| Scenario | Recommendation |
|----------|---------------|
| Large snapshots (>100MB workspaces) | **First choice**: OSS |
| Alibaba Cloud ecosystem | OSS |
| Need sandbox concurrency lock | Mix OSS + Redis |
| Lowest latency | Redis |

## Security

- Use RAM Role + STS temporary credentials in production — avoid hardcoded AK/SK
- Configure bucket lifecycle rules (e.g. 7-day auto-expiry) to control storage costs
