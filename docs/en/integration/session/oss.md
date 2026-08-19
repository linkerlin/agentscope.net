```{note}
This page has been superseded by [Distributed Storage — OSS](../distributed/oss.md). Content below is kept for reference.
```

# OSS State Store

`agentscope-extensions-oss` persists AgentScope agent state in Alibaba Cloud Object Storage Service (OSS). Ideal for large-capacity data and Alibaba Cloud ecosystems.

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Oss" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

## Quickstart

```csharp
using com.aliyun.oss.OSS;
using com.aliyun.oss.OSSClientBuilder;
using AgentScope.core.state.AgentStateStore;
using AgentScope.extensions.oss.OssAgentStateStore;
OSS ossClient = new OSSClientBuilder().Build(endpoint, accessKeyId, accessKeySecret);
AgentStateStore stateStore = OssAgentStateStore.Builder()
    .OssClient(ossClient)
    .BucketName("my-agentscope-bucket")
    .KeyPrefix("agentscope/state/")
    .Build();
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model(model)
    .StateStore(stateStore)
    .Build();
```

## Key layout

The `(userId, sessionId)` pair is packed into OSS object paths:

| Type | Key pattern |
| --- | --- |
| Single value | `{keyPrefix}{userId}/{sessionId}/{stateKey}.json` |
| List | `{keyPrefix}{userId}/{sessionId}/{stateKey}.list.json` |
| List hash | `{keyPrefix}{userId}/{sessionId}/{stateKey}.list.hash` (change detection) |

Anonymous sessions (`userId` is null) use `__anon__` as the user segment.

## Builder reference

| Method | Notes |
| --- | --- |
| `ossClient(OSS)` | Required. Alibaba Cloud OSS client |
| `bucketName(String)` | Required. OSS bucket name |
| `keyPrefix(String)` | Default `agentscope/state/` |

## Security

- Use RAM Role + STS temporary credentials in production — avoid hardcoded AK/SK
- Configure bucket lifecycle rules (e.g. 7-day auto-expiry) to control storage costs
