# OSS 分布式存储

`AgentScope.Extensions.Store.Oss` 基于阿里云 OSS SDK 提供对象存储分布式状态存储实现。

## 安装

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.Oss" Version="2.0.1" />
</ItemGroup>
```

目标框架：net10.0。

## OssDistributedStore

```csharp
using AgentScope.Extensions.Store.Oss;

var ossStore = new OssDistributedStore(
    httpClient,
    endpoint: "oss-cn-hangzhou.aliyuncs.com",
    bucket: "my-bucket",
    accessKeyId: "your-access-key",
    accessKeySecret: "your-access-secret");
```

参数说明：

| 参数 | 说明 |
|------|------|
| `httpClient` | `HttpClient` 实例 |
| `endpoint` | OSS 地域节点，如 `oss-cn-hangzhou.aliyuncs.com` |
| `bucket` | OSS Bucket 名称 |
| `accessKeyId` | RAM 用户 AccessKey ID |
| `accessKeySecret` | RAM 用户 AccessKey Secret |

## OssAgentStateStore

```csharp
using AgentScope.Extensions.Store.Oss;

var ossStore = new OssDistributedStore(httpClient, endpoint, bucket, ak, sk);
var stateStore = new OssAgentStateStore(ossStore);

// 自定义 key 前缀
var stateStore = new OssAgentStateStore(ossStore, keyPrefix: "prod/state");
```

默认 `keyPrefix` 为 `"agentstate"`。

## 与 StateBackedMemory 集成

```csharp
using AgentScope.Core;
using AgentScope.Core.Memory;
using AgentScope.Core.State;
using AgentScope.Extensions.Store.Oss;

var stateStore = new OssAgentStateStore(
    new OssDistributedStore(httpClient, endpoint, bucket, ak, sk));
var initial = new AgentState("demo-session", userId: "alice");
IMemory memory = new StateBackedMemory(stateStore, initial);
```

## 版本化说明

`OssAgentStateStore.SupportsVersioning = false`。OSS 对象存储基于最后写入者胜出（last-writer-wins）语义，不支持 CAS。多副本场景建议配合 Redis/MySQL 使用。

## 生产建议

- 使用 RAM Role + STS 临时凭证替代硬编码 AK/SK。
- 配置 Bucket 生命周期规则（如 7 天自动过期）控制存储成本。
- 对于高频小对象读写，OSS 延迟高于 Redis，建议评估业务场景。
- OSS 适合大容量快照归档场景。

## 相关文档

- [会话状态 — OSS](../session/oss.md) — OssAgentStateStore + Session 使用示例
- [分布式存储总览](index.md) — 后端对比与选型指南
