---
title: "智能体"
description: "了解如何在 AgentScope .NET 2.0 中定义和配置智能体"
---

## 概述

`Agent`（接口位于 `AgentScope.Core.Agent.Agent`，默认实现是 `ReActAgent`）是 AgentScope 的核心抽象——一个推理-行动循环引擎，将模型、工具、权限系统、人机交互、上下文管理、中间件、状态管理和事件系统整合到一个统一接口中。

其主要职责包括：

- 接收输入消息或事件，调用工具完成任务
- 管理上下文（会话历史保存在 `AgentState.Context` 中，可通过 `AgentStateStore` 自动持久化）
- 在关键生命周期阶段提供中间件钩子，支持自定义逻辑
- 自动管理并发和串行工具执行

### 核心接口

`Agent` 接口常用的方法如下：

| 方法 | 描述 |
|------|------|
| `CallAsync(List<Msg>)` / `CallAsync(List<Msg>, RuntimeContext)` | 运行推理-行动循环，返回 `Task<Msg>` |
| `StreamEventsAsync(List<Msg>)` / `StreamEventsAsync(Msg)` | 同 `CallAsync`，但以流式方式逐一产出 `AgentEvent` 对象 |
| `ObserveAsync(Msg)` / `ObserveAsync(List<Msg>)` | 将消息添加到上下文，不触发推理（返回 `Task`） |

`ReActAgent` 在此之上还提供 `CallAsync(msgs, structuredOutputClass, runtimeContext)` 等结构化输出重载，以及通过 `RuntimeContext` 传递 per-call 元数据的便捷入口。

### 主循环

智能体在每次 `CallAsync` 调用时运行推理-行动循环，下图展示了主要控制流程：

```{mermaid}
flowchart TD
    A([输入: 消息 / 事件]) --> B{等待\n外部事件?}
    B -- 是 --> C[处理事件\n更新工具状态]
    B -- 否 --> D[将消息添加到上下文]
    C --> E
    D --> E

    E{检查下一步动作} -- 退出 --> F([返回: 等待\n外部交互])
    E -- 推理 --> G[必要时压缩上下文]
    G --> H[LLM 调用]
    H -- 无工具调用 --> I([返回最终消息])
    H -- 有工具调用 --> Acting

    subgraph Acting [行动]
        direction TB
        J[批量工具调用\n串行 / 并发] --> L[执行工具调用]
        L --> M{权限\n检查}
        M -- 允许 --> N[运行工具 → 结果]
        M -- 询问 / 外部 --> O([暂停并发出\nRequireUserConfirmEvent])
        M -- 拒绝 --> P[将错误结果返回 LLM]
    end

    N --> E
    P --> E
```

## 配置智能体

通过 `ReActAgent.Builder()...Build()` 创建智能体。`.Model(...)` 既接受 `ModelRegistry` 解析的字符串 id（最常用、自动读取 env），也接受手动 builder 构造的 `Model` 实例（需要精细控制超时、自定义 endpoint 时用）。

::::{tab-set}
:::{tab-item} 字符串 model id（推荐）
```csharp
using AgentScope.Core;
using AgentScope.Core.Tool;

ReActAgent agent =
        ReActAgent.Builder()
                .WithName("my_agent")
                .WithSysPrompt("你是一个有帮助的助手。")
                // 由 ModelRegistry 解析；自动读取 DASHSCOPE_API_KEY
                // 切换其他厂商时改成 "openai:gpt-5.5" / "anthropic:claude-sonnet-4-5"
                // / "deepseek:deepseek-v4-flash" / "gemini:gemini-2.0-flash" / "ollama:llama3" 即可。
                .WithModel("dashscope:qwen-plus")
                .WithToolkit(new Toolkit())
                .Build();
```
:::
:::{tab-item} 显式 Model builder
```csharp
using AgentScope.Core;
using AgentScope.Extensions.Model.DashScope;
using AgentScope.Extensions.Model.DashScope.Formatter;
using AgentScope.Core.Tool;

ReActAgent agent =
        ReActAgent.Builder()
                .WithName("my_agent")
                .WithSysPrompt("你是一个有帮助的助手。")
                .WithModel(
                        DashScopeChatModel.Builder()
                                .WithApiKey("YOUR_API_KEY")
                                .WithModelName("qwen-max")
                                .WithStream(true)
                                .WithFormatter(new DashScopeChatFormatter())
                                .Build())
                .WithToolkit(new Toolkit())
                .Build();
```
:::
:::{tab-item} 配置 Toolkit / MCP
```csharp
using AgentScope.Core;
using AgentScope.Core.Tool;
using AgentScope.Core.Tool.BuiltIn;
using AgentScope.Core.Tool.Mcp;

Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new TodoTools());          // 通过反射注册带 [Tool] 的方法
toolkit.RegisterTool(new MyCustomTools());      // 自定义工具类（带 [Tool] 注解的方法）

McpClientWrapper amap = await McpClientBuilder.StreamableHttp()
        .WithName("amap")
        .WithUrl("https://mcp.amap.com/mcp?key=" + Environment.GetEnvironmentVariable("AMAP_API_KEY"))
        .BuildAsync();
toolkit.RegisterMcpClient(amap);

ReActAgent agent =
        ReActAgent.Builder()
                .WithName("my_agent")
                .WithSysPrompt("你是一个有帮助的助手。")
                .WithModel("dashscope:qwen-max")
                .WithToolkit(toolkit)
                .Build();
```
:::
::::

:::{tip}
`ModelRegistry` 的字符串形式（`<provider>:<model>`）需要对应的模型扩展模块在 classpath 中。它支持 `dashscope` / `openai` / `deepseek` / `anthropic` / `gemini` / `ollama`，会自动从环境变量读取 API key（`DASHSCOPE_API_KEY` / `OPENAI_API_KEY` / `DEEPSEEK_API_KEY` / `ANTHROPIC_API_KEY` / `GEMINI_API_KEY`）。需要在长期运行场景下同时获得工作区、会话持久化、记忆压缩、子 agent 等能力，请改用 [`HarnessAgent`](../harness/architecture.md) —— 它对 `ReActAgent` 做了一层薄包装，builder 接口大体一致。
:::

### 参数说明

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `Name` | `string` | 必填 | 智能体标识符，用于消息和日志 |
| `SysPrompt` | `string` | 必填 | 智能体的基础系统提示词 |
| `Model` | `Model` | 必填 | 用于推理的大语言模型（继承自 `ChatModelBase`） |
| `Toolkit` | `Toolkit` | `new Toolkit()` | 管理工具、MCP 客户端、技能和工具组 |
| `Middlewares` | `List<IMiddlewareBase>` | `new List<IMiddlewareBase>()` | 应用于 agent / reasoning / acting / model call / system prompt 钩子 |
| `StateStore` | `AgentStateStore` | `null`（不持久化） | 配置后 agent 在每次 `CallAsync` 后自动加载/保存 `AgentState`，按该次调用 `RuntimeContext` 的 `(UserId, SessionId)` 寻址 |
| `DefaultSessionId` | `string` | agent `Name` | 当某次调用的 `RuntimeContext` 没带 `SessionId` 时的兜底值 |
| `PermissionContext` | `PermissionContextState` | 默认 `Default` 模式 | 工具执行的细粒度规则，参见 [权限系统](./permission-system.md) |
| `ModelConfig` | `ModelConfig` | 默认值 | 模型重试次数和备用模型 |
| `ReactConfig` | `ReactConfig` | 默认值 | 最大迭代次数和拒绝处理方式 |
| `MaxIters` | `int` | `10` | ReAct 主循环最大迭代次数（也可放在 `ReactConfig` 中） |

## 多用户 / 多会话并发

`ReActAgent` 在调用之间**是无状态的**——同一个 agent 实例可以同时服务多个用户和会话。每次 `CallAsync()` 通过 `RuntimeContext` 携带的 `(UserId, SessionId)` 来定位该次调用应该使用哪份对话状态，互不干扰。

```csharp
using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.State;

// 应用启动时创建一个 agent 实例（单例）
ReActAgent agent = ReActAgent.Builder()
        .WithName("assistant")
        .WithSysPrompt("你是一个有帮助的助手。")
        .WithModel("dashscope:qwen-plus")
        .WithStateStore(new JsonFileAgentStateStore(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".agentscope/sessions")))
        .Build();

// 在 HTTP handler 中——不同请求传入不同 RuntimeContext，各自隔离
await agent.CallAsync(new List<Msg> { new UserMessage("你好") },
        RuntimeContext.Builder().WithUserId("alice").WithSessionId("session-1").Build());

await agent.CallAsync(new List<Msg> { new UserMessage("Hi there") },
        RuntimeContext.Builder().WithUserId("bob").WithSessionId("session-2").Build());
```

每次 `CallAsync()` 开始时，agent 根据 `RuntimeContext` 中的 `(UserId, SessionId)` 自动加载对应的 `AgentState`（对话上下文、权限规则等）；call 结束后自动保存。不同 session 的状态完全隔离。

:::{tip}
同一个 `(UserId, SessionId)` 的调用会按到达顺序**串行化执行**——第二个请求等待第一个完成后再开始。不同 session 的调用可以完全并行。
:::

Spring Boot 完整示例见 `agentscope-examples/documentation/.../streaming/StreamingWebExample.java`。

## 中断执行（Interrupt）

当需要从外部中断一个正在运行的 agent call 时（用户取消、超时、优雅停机），使用 `Interrupt`：

```csharp
using AgentScope.Core.Agent;

// 构造标识目标 session 的 RuntimeContext
RuntimeContext target = RuntimeContext.Builder()
        .WithUserId("alice")
        .WithSessionId("session-001")
        .Build();

// 中断该 session 正在进行的 call
agent.Interrupt(target);

// 带消息中断——中断消息会被 LLM 在恢复时看到
agent.Interrupt(target, new UserMessage("用户已取消操作"));
```

中断是 **per-session** 的：只影响指定 `(UserId, SessionId)` 的 in-flight call，不会波及同一 agent 上其他 session 的并发请求。

**中断后的行为：**
- 当前推理/工具执行在下一个检查点（reasoning 开始、acting 开始、streaming 每个 chunk）被拦截
- agent 返回一个带 `GenerateReason.Interrupted` 标记的 Msg
- 对话上下文（AgentState）自动保存——下次对同一 session 发起 `CallAsync()` 时从中断点恢复

也可以直接用 `(userId, sessionId)` 字符串：

```csharp
agent.Interrupt("alice", "session-001");
agent.Interrupt("alice", "session-001", interruptMsg);
```

## 运行智能体

`CallAsync` 和 `StreamEventsAsync` 都接受相同的输入消息列表，驱动相同的推理-行动循环，区别在于结果的交付方式。

### CallAsync

`CallAsync` 在内部消费所有事件，当智能体完成或因外部交互暂停时返回最终 `Msg`。

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

UserMessage msg = new UserMessage("当前目录有哪些文件？");
Msg result = await agent.CallAsync(new List<Msg> { msg }, RuntimeContext.Empty());
Console.WriteLine(result.TextContent);
```

### StreamEventsAsync

`StreamEventsAsync` 逐一产出 `AgentEvent` 对象，让你实时将文本输出、工具调用进度和生命周期事件流式传输给用户。按 `event.Type` 分发即可针对每类事件做不同处理：

```csharp
using AgentScope.Core.Event;

await foreach (var evt in agent.StreamEventsAsync(new UserMessage("总结一下 README 的内容。")))
{
    if (evt.Type == AgentEventType.TextBlockDelta)
    {
        // 模型返回的流式文本片段 —— 追加到界面或标准输出
        Console.Write(((TextBlockDeltaEvent)evt).Delta);
    }
    else if (evt.Type == AgentEventType.ToolCallStart)
    {
        // 智能体即将调用工具 —— 展示调用信息
        Console.WriteLine("\n[tool] " + ((ToolCallStartEvent)evt).ToolCallName);
    }
    // 其他事件：思考块、工具结果、回复结束等
}
```

完整事件类型与字段参考 [消息与事件](./message-and-event.md)。

### ObserveAsync

使用 `ObserveAsync` 将消息注入智能体上下文而不触发 reply——适用于多智能体场景中，一个智能体需要观察另一个智能体输出的情况。

```csharp
await agent.ObserveAsync(otherAgentMsg);
```

## RuntimeContext (per-call 上下文)

`RuntimeContext`（`AgentScope.Core.Agent.RuntimeContext`）是 **per-call 元数据袋**：每次 `CallAsync` / `Stream` 把一份实例传进去，agent 在执行期间把它绑定到自身，下游的工具、middleware、hook 都能读到同一份引用；调用结束后自动解绑。

它**不是**持久化状态——`AgentState`（聊天上下文、压缩摘要、权限规则、tool state）才是。`RuntimeContext` 的作用是承载「当前这一次调用」相关的瞬态数据：tenant / userId / request-id、DB 连接、审计 logger、特性开关，等等。

### 内置字段与属性层

`RuntimeContext` 有三类「槽位」：

| 槽位 | 设置方式 | 读取方式 |
|------|---------|---------|
| 会话字段 | `SessionId(string)` / `UserId(string)` | `SessionId` / `UserId` |
| 字符串属性（任意 key-value） | `Put(string key, object value)` | `T Get<T>(string key)` |
| 类型化属性（按 `Type` 注入业务 POJO） | `Put<T>(T value)` / `Put<T>(string key, T value)` | `T Get<T>()` / `T Get<T>(string key)` |

类型化属性是给 tool 用的——`[Tool]` 方法里声明同类型参数即可被框架自动注入，详见 [Tool — 接收 Context](./tool.md#接收-context)。字符串属性通常用于内部协调（例如 middleware 之间传值）。两层互不串扰：类型化层放进去的对象不会出现在 `Extra` 里，反之亦然。

### 构造并传入

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

RuntimeContext ctx =
        RuntimeContext.Builder()
                .WithUserId("alice")                           // 可选；null 表示匿名
                .WithSessionId("session-001")                  // 选择状态槽位
                .Put("request_id", "req-abc-123")              // 字符串层
                .Put(new UserContext("alice", "en"))  // 类型层（业务 POJO）
                .Build();

Msg result = await agent.CallAsync(new List<Msg> { new UserMessage("Hi.") }, ctx);
```

`ReActAgent` 提供 `CallAsync` / `Stream` 的 `RuntimeContext` 重载；`StreamEventsAsync` 未直接重载，需要传 context 时改用 `Stream(msgs, options, ctx)` 或先在 builder 上配置全局 `toolExecutionContext`。不传 context 时框架使用 `RuntimeContext.Empty()`，会话字段为 `null`，属性表为空，此时 agent 回退到 builder 上配置的 `DefaultSessionId`。

### 谁能读到

- **Tool**（`[Tool]` 方法或 `ToolBase.CallAsync`）—— 见 [Tool — 接收 Context](./tool.md#接收-context)。
- **Middleware**（`IMiddlewareBase` 所有 hook）—— 作为第二个参数 `ctx` 直接传入。详见 [Middleware — 读取 RuntimeContext](./middleware.md#读取-runtimecontext)。
- **同一次调用的所有线程**—— `RuntimeContext` 内部使用 `ConcurrentDictionary`，hook / tool 之间可以读写同一实例做协调。

### 与持久化的关系

- `RuntimeContext` 的自由 / 类型属性**不会**进 `AgentState`，也不会被 `AgentStateStore` 写回磁盘。
- `SessionId` / `UserId` 字段**会**驱动持久化：每次调用激活对应的 `(UserId, SessionId)` 状态槽位，因此在 `RuntimeContext` 上传不同身份就会切换加载/保存的 `AgentState`。不传时回退到 builder 上配置的 `DefaultSessionId`。

完整示例：`agentscope-examples/documentation/.../context/RuntimeContextExample.java`、`tool/ToolExecutionContextExample.java`。

:::{note}
存在一个旧的 `ToolExecutionContext`（`AgentScope.Core.Tool`），已标记 `[Obsolete]`，新代码统一使用 `RuntimeContext`。它在底层会被自动桥接到 `RuntimeContext.AsToolExecutionContext()`，老代码不会立即失效。
:::

## 人机交互

当智能体遇到以下两种情况时，会暂停执行并发出特殊事件：需要**用户确认**的工具调用（权限系统返回 ASK），或标记为**外部执行**的工具（结果必须来自智能体外部）。两种情况下，都可以通过把结果事件再次喂给 agent 的下一次 `CallAsync` 来恢复执行。

### 用户确认

当权限系统判断某个工具调用需要用户批准时，智能体会发出 `RequireUserConfirmEvent` 并暂停。

**1. 接收 `RequireUserConfirmEvent`** —— 用 `StreamEventsAsync` 监听暂停。事件携带 `ReplyId`（用于恢复）和 `ToolCalls` —— 一组 `ToolUseBlock`，每个暴露 `Id` / `Name` / `Input` / `SuggestedRules`。

```csharp
using AgentScope.Core.Event;

await foreach (var evt in agent.StreamEventsAsync(msg))
{
    if (evt is RequireUserConfirmEvent confirm)
    {
        foreach (var tc in confirm.ToolCalls)
        {
            Console.WriteLine("工具: " + tc.Name + ", 输入: " + tc.Input);
            Console.WriteLine("建议规则: " + tc.SuggestedRules);
        }
    }
}
```

**2. 构建确认结果** —— 为每个待处理工具调用构造一个 `ConfirmResult`。可以在传回前修改工具输入，或接受 suggested rules 让今后相同的调用自动放行：

```csharp
using AgentScope.Core.Event;
using System.Collections.Generic;

List<ConfirmResult> confirmResults = new List<ConfirmResult>();
foreach (var tc in confirmEvent.ToolCalls)
{
    confirmResults.Add(
            new ConfirmResult(
                    /* confirmed = */ true,                  // false 表示拒绝
                    /* toolCall  = */ tc,                    // 传回（可选择修改）
                    /* rules     = */ tc.SuggestedRules // 接受规则 → 未来调用自动放行
                    ));
}
```

**3. 恢复智能体** —— 将 `confirmResults` 通过 metadata 传给下一次 `CallAsync`：

```csharp
using AgentScope.Core.Message;

UserMessage resumeMsg =
        UserMessage.Builder()
                .WithMetadata(new Dictionary<string, object>
                {
                    { Msg.MetadataConfirmResults, confirmResults }
                })
                .Build();

Msg result = await agent.CallAsync(new List<Msg> { resumeMsg }, RuntimeContext.Empty());
```

- **已确认**的工具调用立即执行，智能体继续推理。
- **已拒绝**的工具调用会产生 LLM 可见的错误结果，LLM 可能会用不同方式重试。
- **已接受的规则**会持久化到权限引擎中——匹配的未来调用将自动允许，无需再次提示。

### 外部工具执行

当智能体调用 `IsExternalTool() == true` 的工具时，会发出 `RequireExternalExecutionEvent` 并暂停。工具的逻辑在智能体外部运行——通常由人工操作员或外部系统执行。

**1. 接收 `RequireExternalExecutionEvent`** —— 结构与用户确认一致：`ReplyId` 加一组等待外部执行的 `ToolCalls`。

```csharp
using AgentScope.Core.Event;

await foreach (var evt in agent.StreamEventsAsync(msg))
{
    if (evt is RequireExternalExecutionEvent ext)
    {
        foreach (var tc in ext.ToolCalls)
            Console.WriteLine("外部执行: " + tc.Name + "(" + tc.Input + ")");
    }
}
```

**2. 外部执行并构建结果** —— 在智能体外部完成操作，把每个结果封装为 `ToolResultBlock`：

```csharp
using AgentScope.Core.Message;
using System.Collections.Generic;

List<ToolResultBlock> executionResults = new List<ToolResultBlock>();
foreach (var tc in externalEvent.ToolCalls)
{
    string output = RunExternalOperation(tc.Name, tc.Input);
    executionResults.Add(
            ToolResultBlock.Builder()
                    .WithId(tc.Id)
                    .WithName(tc.Name)
                    .WithOutput(new List<ContentBlock> { TextBlock.Builder().WithText(output).Build() })
                    .WithState(ToolResultState.Success)
                    .Build());
}
```

**3. 恢复智能体** —— 将结果作为下一次 `CallAsync` 的输入消息回传。结果校验通过后会被注入智能体上下文，agent 会先发出 `ExternalExecutionResultEvent`，其 `ReplyId` 与之前的 `RequireExternalExecutionEvent.ReplyId` 相同，然后从中断处继续推理。

:::{tip}
构建交互式 UI 时使用 `StreamEventsAsync`——它可以实时检测暂停事件并立即提示用户。以编程方式处理事件的自动化流程则使用 `CallAsync`。完整可运行示例见 `agentscope-examples/documentation/.../hitl/PermissionHITLExample.java`。
:::

## 配置状态持久化（AgentStateStore）

`AgentState` 是 agent 的全部可恢复状态——对话上下文、压缩摘要、权限规则、工具状态和当前 reply 位置。[`AgentStateStore`](../../integration/session/index.md) 是它的存储抽象。

**只需在 builder 上配 `StateStore(...)`，agent 就会自动持久化与恢复**：每次 `CallAsync` 结束把 `AgentState` 写回，下次用同一 `(UserId, SessionId)` 调用时自动加载。Agent 实例本身对 session 无状态——具体读写哪个槽位由该次调用的 `RuntimeContext` 决定（缺省回退到 `DefaultSessionId`）。

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.State;

ReActAgent agent = ReActAgent.Builder()
        .WithName("my_agent")
        .WithSysPrompt("你是一个有帮助的助手。")
        .WithModel(model)
        .WithToolkit(new Toolkit())
        .WithStateStore(new JsonFileAgentStateStore(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".agentscope/sessions")))
        .Build();

// 选定本次会话的槽位；userId 可选（null 表示匿名）
RuntimeContext rc = RuntimeContext.Builder()
        .WithUserId("user_123")
        .WithSessionId("session_789")
        .Build();

// 若 (user_123, session_789) 有历史数据会自动加载；调用结束自动持久化
await agent.CallAsync(new List<Msg> { new UserMessage("继续之前的任务。") }, rc);
```

内置与扩展实现：

| 实现 | 模块 | 适用 |
|------|------|------|
| `InMemoryAgentStateStore` | `AgentScope.Core` | 单元测试 / 单进程 demo |
| `JsonFileAgentStateStore` | `AgentScope.Core` | 单机开发，按 `(UserId, SessionId)` 分目录落 JSON |
| `RedisAgentStateStore` | `AgentScope.Extensions.Redis` | 多副本生产，跨进程跨机器共享 |
| `MysqlAgentStateStore` | `AgentScope.Extensions.MySql` | 需要落关系型库（审计 / 报表） |

大多数场景只用一个 `SessionId` 就够；要按用户分桶就在 `RuntimeContext` 上同时设置 `UserId`，存储会按 `(UserId, SessionId)` 二元组寻址每个槽位。

通过 `agent.GetAgentState(userId, sessionId)` 或 `agent.GetAgentState(runtimeContext)` 可读取指定会话的状态快照：

```csharp
AgentState state = agent.GetAgentState("alice", "session-001");
int count = state.Context.Count;                  // 当前对话消息数
string json = state.ToJson();                     // 序列化为 JSON
```

完整字段、跨节点接续见[上下文与 AgentState](context.md)；压缩 / Plan Mode / 子 agent 的协作细节见[上下文压缩](../harness/compaction.md)。

## 结构化输出

结构化输出让智能体按照你指定的 JSON Schema 返回结果，而不是自由文本。适用于需要程序化消费 agent 输出的场景——表单填写、数据提取、决策分类等。

### 基本用法

传一个 C# 类（或 `JsonDocument` schema）给 `CallAsync` 即可：

```csharp
using AgentScope.Core.Message;

// 定义输出结构
public record WeatherResponse(string Location, string Temperature, string Condition);

Msg result = await agent.CallAsync(new List<Msg> { new UserMessage("旧金山天气如何？") }, typeof(WeatherResponse));

// 从结果中取出强类型数据
WeatherResponse weather = result.GetStructuredData<WeatherResponse>();
Console.WriteLine(weather.Location);      // "San Francisco"
Console.WriteLine(weather.Temperature);   // "18°C"
```

结构化输出与工具可以同时使用——智能体会先调用工具完成任务，最后以指定 schema 输出最终结果。

### 工作原理

框架根据模型能力自动选择实现路径：

| 路径 | 条件 | 行为 |
|------|------|------|
| **原生路径** | 模型支持 `response_format` + tools 并行（OpenAI、DashScope 等） | 将 JSON Schema 通过 `response_format` 直接传给模型 API，模型保证输出合法 JSON，循环自然结束 |
| **降级路径** | 模型不支持原生结构化输出（Anthropic、Ollama 等） | 注入 `generate_response` 合成工具 + 指令提示，模型以 tool call 方式输出结构化结果 |

无论哪条路径，调用方的代码完全相同——路径选择对用户透明。

```
┌─── CallAsync(msgs, Schema.class) ───┐
│                                │
│   model.supportsNative...?     │
│      ├─ yes → response_format  │  ← 零额外开销，模型原生保证
│      └─ no  → generate_response│  ← 合成工具 + instruction
│                                │
└──── 返回带 schema 数据的 Msg ──┘
```

### 从结果中读取数据

`CallAsync` 返回的 `Msg` 在 metadata 中携带解析后的结构化数据：

```csharp
// 方式一：强类型提取
WeatherResponse data = result.GetStructuredData<WeatherResponse>();

// 方式二：作为 Dictionary 读取
Dictionary<string, object> map = (Dictionary<string, object>)result.Metadata["_structured_output"];
```

### 使用 JsonDocument Schema

如果不想定义 C# 类，可以直接传 JSON Schema：

```csharp
using System.Text.Json;

JsonDocument schema = JsonDocument.Parse("""
    {
      "type": "object",
      "properties": {
        "sentiment": { "type": "string", "enum": ["positive", "negative", "neutral"] },
        "confidence": { "type": "number" }
      },
      "required": ["sentiment", "confidence"]
    }
    """);

Msg result = await agent.CallAsync(new List<Msg> { new UserMessage("分析这段评论的情感") }, schema);
```

## 更多能力

以下能力均通过 builder 配置，详情参见各自的文档页面：

### 模型容错

```csharp
ReActAgent.Builder()
        .WithModel("dashscope:qwen-plus")
        .WithMaxRetries(3)                              // 模型调用失败时自动重试
        .WithFallbackModel("dashscope:qwen-max")        // 主模型连续失败后切换到备用模型
        .Build();
```

### 技能系统（Skills）

技能是可热加载的 Markdown 提示词模块，运行时由 LLM 按需激活：

```csharp
ReActAgent.Builder()
        .WithSkillRepository(new MysqlSkillRepository(dataSource))
        .Build();
```

### 内置工具

| Builder 方法 | 说明 |
|---|---|
| `EnableMetaTool(true)` | 注册 `list_tools` / `activate_group` 元工具，让 LLM 能发现和切换工具组 |
| `EnableTaskList()` | 注册任务列表工具，让 LLM 拆解复杂任务为步骤并逐步完成 |

## 延伸阅读

::::{grid} 2

:::{grid-item-card} 权限系统
:link: ./permission-system.html

控制智能体可以调用哪些工具以及在什么条件下调用。
:::

:::{grid-item-card} 中间件
:link: ./middleware.html

在 agent、reasoning、acting 和 model call 钩子处拦截和修改智能体行为。
:::

::::
