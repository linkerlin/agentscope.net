```{note}
本页面内容已迁移至 [分布式存储 — OSS](../distributed/oss.md)。以下内容保留作为参考，但建议使用新文档。
```

# OSS 状态存储

`agentscope-extensions-oss` 把 AgentScope 的 Agent 状态持久化到阿里云对象存储（OSS）。适合大容量数据和阿里云生态的场景。

## 添加依赖

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Oss" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

## 快速上手

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

## Key 结构

`(userId, sessionId)` 二元组会被打包进 OSS 对象路径：

| 类型 | Key 模式 |
| --- | --- |
| 单值 | `{keyPrefix}{userId}/{sessionId}/{stateKey}.json` |
| 列表 | `{keyPrefix}{userId}/{sessionId}/{stateKey}.list.json` |
| 列表 hash | `{keyPrefix}{userId}/{sessionId}/{stateKey}.list.hash`（变更检测用） |

匿名 session（`userId` 为 null）时 `userId` 用 `__anon__` 替代。

## Builder 配置参数

| 方法 | 说明 |
| --- | --- |
| `ossClient(OSS)` | 必填。阿里云 OSS 客户端 |
| `bucketName(String)` | 必填。OSS Bucket 名称 |
| `keyPrefix(String)` | 默认 `agentscope/state/` |

## 安全提示

- 生产环境建议使用 RAM Role + STS 临时凭证，避免在代码中硬编码 AK/SK
- 为 bucket 配置生命周期规则（如 7 天自动过期），避免存储成本失控
