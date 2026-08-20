# OSS Distributed Storage

`AgentScope.Extensions.Store.Oss` provides object-storage-based distributed state storage backed by Alibaba Cloud OSS SDK.

## Dependency

```xml
<ItemGroup>
  <PackageReference Include="AgentScope.Extensions.Store.Oss" Version="2.0.1" />
</ItemGroup>
```

Target framework: net10.0.

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

Parameter reference:

| Parameter | Description |
|-----------|-------------|
| `httpClient` | `HttpClient` instance |
| `endpoint` | OSS region endpoint, e.g. `oss-cn-hangzhou.aliyuncs.com` |
| `bucket` | OSS bucket name |
| `accessKeyId` | RAM user AccessKey ID |
| `accessKeySecret` | RAM user AccessKey Secret |

## OssAgentStateStore

```csharp
using AgentScope.Extensions.Store.Oss;

var ossStore = new OssDistributedStore(httpClient, endpoint, bucket, ak, sk);
var stateStore = new OssAgentStateStore(ossStore);

// Custom key prefix
var stateStore = new OssAgentStateStore(ossStore, keyPrefix: "prod/state");
```

The default `keyPrefix` is `"agentstate"`.

## Integration with StateBackedMemory

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

## Versioning Notes

`OssAgentStateStore.SupportsVersioning = false`. OSS uses last-writer-wins semantics and does not support CAS. For multi-replica deployments, consider combining with Redis or MySQL.

## Production Considerations

- Use RAM Role + STS temporary credentials instead of hardcoded AK/SK.
- Configure bucket lifecycle rules (e.g. 7-day auto-expiry) to control storage costs.
- OSS latency is higher than Redis for frequent small-object reads/writes — evaluate your use case.
- OSS is ideal for large-capacity snapshot archiving scenarios.

## Related Documentation

- [Session State — OSS](../session/oss.md) — OssAgentStateStore + Session usage examples
- [Distributed Storage Overview](index.md) — Backend comparison and selection guide
