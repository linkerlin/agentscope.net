# Bailian Memory

`agentscope-extensions-memory-bailian` integrates with Alibaba Cloud Bailian's long-term memory service. It is fully managed and supports advanced retrieval features such as rerank, judge, and rewrite.

## When to use

- You are already on Alibaba Cloud Bailian and want to reuse memory libraries from the platform.
- You care about retrieval quality and want to use Bailian's rerank / judge / rewrite pipeline.
- You need three-level isolation via `userId` + `memoryLibraryId` + `projectId`.

## Add the dependency

```xml
<ItemGroup>
    <PackageReference Include="AgentScope.Extensions.Memory.Bailian" Version="$(AgentScopeVersion)" />
</ItemGroup>
```

## Quickstart

```csharp
using AgentScope.core.memory.bailian.BailianLongTermMemory;
using (BailianLongTermMemory memory = BailianLongTermMemory.Builder()
        .ApiKey(Environment.GetEnvironmentVariable("DASHSCOPE_API_KEY"))
        .UserId("user_001")
        .MemoryLibraryId("lib_xxxxx")
        .ProjectId("proj_xxxxx")
        .Build()) {
    ReActAgent agent = ReActAgent.Builder()
        .Name("Assistant")
        .Model(model)
        .LongTermMemory(memory)
        .LongTermMemoryMode(LongTermMemoryMode.BOTH)
        .Build();
    agent.Call(new UserMessage("Remind me to drink water at 9am every day")).GetAwaiter().GetResult();
}
```

`BailianLongTermMemory` implements `IDisposable` — a `using` statement is recommended so the underlying HTTP connections get released.

## Retrieval feature switches

On top of basic recall, Bailian supports three pipeline switches:

```csharp
BailianLongTermMemory memory = BailianLongTermMemory.Builder()
    .ApiKey(apiKey)
    .UserId("user_001")
    .MemoryLibraryId("lib_xxxxx")
    .TopK(20)
    .MinScore(0.4)
    .EnableRerank(true)   // Re-rank results, more accurate but slower
    .EnableJudge(true)    // Let an LLM judge whether results are actually relevant
    .EnableRewrite(true)  // Rewrite/merge memories on write
    .Build();
```

Leave them off by default unless you need them — they add latency and cost.

## Message filtering

Bailian memory only stores natural user/assistant exchanges:

- Only `MsgRole.USER` and `MsgRole.ASSISTANT` messages are written.
- Assistant messages containing `ToolUseBlock` (tool-call requests) are skipped.
- Messages with the `<compressed_history>` marker are skipped to avoid storing duplicated compressed history.

If you need tool results to enter memory, write them yourself via higher-level logic before calling `record(...)`.

## Builder reference

| Method | Required | Default | Notes |
| --- | --- | --- | --- |
| `apiKey(String)` | ✅ | - | Bailian DashScope API key |
| `userId(String)` | ✅ | - | User-level ID |
| `memoryLibraryId(String)` | ❌ | - | Memory library ID |
| `projectId(String)` | ❌ | - | Project ID |
| `profileSchema(String)` | ❌ | - | User profile schema ID |
| `apiBaseUrl(String)` | ❌ | `https://dashscope.aliyuncs.com` | Override for custom gateways |
| `topK(Integer)` | ❌ | `10` | Maximum number of retrieved items |
| `minScore(Double)` | ❌ | `0.3` | Minimum similarity threshold (0–1) |
| `enableRerank(Boolean)` | ❌ | `false` | Enable rerank |
| `enableJudge(Boolean)` | ❌ | `false` | Enable LLM judge |
| `enableRewrite(Boolean)` | ❌ | `false` | Enable rewrite on write |
| `metadata(Map)` | ❌ | - | Custom metadata stored with each memory |
| `httpTransport(HttpTransport)` | ❌ | default | Replace the HTTP client |
