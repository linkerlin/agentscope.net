---
title: "Middleware"
description: "在 agent 生命周期的关键位置拦截并扩展行为"
---

## 概述

Agent middleware 是在不修改 agent 或 model 代码的前提下，向 agent 执行流程中的关键位置注入自定义逻辑（日志、追踪、输入改写、访问控制等）的机制。

AgentScope .NET 中，可以在 5 个位置上设置 hook，覆盖了从外层 reply 流程一路下沉到底层模型 API 调用的全链路：

| 位置 | 类型 | 说明 |
|------|------|------|
| `OnAgent` | Onion | 包裹一次完整的 reply 流程，覆盖其中所有 ReAct 轮次、工具执行与最终输出 |
| `OnReasoning` | Onion | 包裹一轮 ReAct 中的推理步骤（输入组装 → 模型调用 → 流式解码） |
| `OnActing` | Onion | 包裹一次工具调用的执行 |
| `OnModelCall` | Onion | 包裹一次底层 `ChatModel` API 调用，最贴近模型 |
| `OnSystemPrompt` | Transformer | 在每次组装 system prompt 时触发；多个 middleware 串行接力，每一个把上一个的输出再做一次变换 |

两种类型的差别：

- **Onion**（洋葱式）—— middleware 包裹下一层 handler，可以在 `next.Apply(input)` 前后插入逻辑、观察中间事件流。
- **Transformer**（变换式）—— middleware 之间串成流水线，前一个的输出作为后一个的输入，不存在「内层」概念。

下图展示这些 hook 在 agent 生命周期中的嵌套关系。`OnSystemPrompt` 嵌入在 `OnReasoning` 内部，因为它在 reasoning 步骤组装 system prompt 时被触发：

```text
OnAgent/
└── ReAct loop（每一轮）/
    ├── OnReasoning/
    │   ├── OnSystemPrompt（组装 system prompt）
    │   └── OnModelCall（模型 API 调用）
    └── OnActing（每次工具调用）
```

:::{note}
当前 `OnActing` 只包裹 agent 运行时内部的工具执行；通过 external execution 在 agent 外部执行的工具不会被 `OnActing` 追踪到。
:::

## 装备 Middleware

AgentScope 把一组 hook 装在一个 `IMiddlewareBase` 实现里 —— 同一个 middleware 类可以同时实现 5 个位置中任意子集的 hook（其余位置默认 `next.Apply(input)`）。把实例传给 builder 的 `Middlewares(...)` 即可装备：

```csharp
using AgentScope.Core;
using AgentScope.Core.Middleware;
using AgentScope.Core.Tracing;
using System.Collections.Generic;

ReActAgent agent =
        ReActAgent.Builder()
                .WithName("assistant")
                .WithSysPrompt("You are a helpful assistant.")
                .WithModel(model)
                .WithToolkit(toolkit)
                .WithMiddlewares(new List<IMiddlewareBase> { new OtelTracingMiddleware() })
                .Build();
```

`Middleware(...)`（单数）也可单独添加一个；`Middlewares(...)` 接受 `List<IMiddlewareBase>`，未实现的位置自动跳过，不产生任何调用开销。

## 内置 Middleware

### OtelTracingMiddleware

`OtelTracingMiddleware`（位于 `AgentScope.Core.Tracing`）为 agent 全生命周期接入 [OpenTelemetry](https://opentelemetry.io/docs/specs/semconv/gen-ai/) 追踪。它在 `OnAgent`、`OnModelCall`、`OnActing` 三个位置打点，按层级生成 span：

- `invoke_agent <name>` —— 包裹整次 reply
- `chat <model>` —— 包裹每次模型 API 调用
- `execute_tool <name>` —— 包裹每次工具执行

未配置 OpenTelemetry SDK（只剩默认的 no-op provider）时，所有 hook 会直接短路到 `next.Apply(input)`，几乎零开销。

使用前先在进程中初始化 OpenTelemetry SDK（OTLP exporter、`SdkTracerProvider`、`OpenTelemetrySdk.Builder().SetTracerProvider(...).BuildAndRegisterGlobal()`），随后把 `OtelTracingMiddleware` 装到 agent 上即可：

```csharp
using AgentScope.Core;
using AgentScope.Core.Tracing;
using System.Collections.Generic;

ReActAgent agent =
        ReActAgent.Builder()
                .WithName("assistant")
                .WithSysPrompt("You are a helpful assistant.")
                .WithModel(model)
                .WithToolkit(toolkit)
                .WithMiddlewares(new List<IMiddlewareBase> { new OtelTracingMiddleware() })
                .Build();
```

每次 reply 会产出一棵嵌套 span 树，关键属性包括 agent 名称、session ID、模型名、token 数、工具名与入参等。

### TaskReminderMiddleware

`TaskReminderMiddleware`（位于 `AgentScope.Core.Middleware`）与内置 `TodoTools` 配合使用，在每个 reasoning step 之前把当前 `AgentState.TasksContext` 渲染成 `<system-reminder>` 注入上下文，避免长任务期间 agent 偏离计划。

通过 builder 上的 `EnableTaskList(true)` 开关与 `TodoTools` 一同启用：

```csharp
using AgentScope.Core;
using AgentScope.Core.Tool;
using AgentScope.Core.Tool.BuiltIn;

Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new TodoTools());

ReActAgent agent =
        ReActAgent.Builder()
                .WithName("planner")
                .WithSysPrompt("You plan tasks step by step.")
                .WithModel(model)
                .WithToolkit(toolkit)
                .EnableTaskList(true)
                .Build();
```

## 自定义 Middleware

实现 `IMiddlewareBase` 接口（位于 `AgentScope.Core.Middleware`），只重写需要的 hook 即可，其它的不用管。

每个洋葱 hook 收到一个 `next` 函数，调用 `next.Apply(input)` 进入内层逻辑；可以在调用前后插入自己的处理，或者通过 `IAsyncEnumerable<AgentEvent>` 算子观察、改写中间事件流。

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>同时观察 agent / reasoning / model_call / system_prompt 四个位置。</summary>
public class FullObservabilityMiddleware : IMiddlewareBase
{
    public IAsyncEnumerable<AgentEvent> OnAgent(
            Agent agent, RuntimeContext ctx, AgentInput input,
            Func<AgentInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Console.WriteLine("[agent] start for " + agent.Name);
        return IterateWithComplete(next(input), () => Console.WriteLine("[agent] end for " + agent.Name));
    }

    public IAsyncEnumerable<AgentEvent> OnReasoning(
            Agent agent, RuntimeContext ctx, ReasoningInput input,
            Func<ReasoningInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Console.WriteLine("[reasoning] start");
        return IterateWithComplete(next(input), () => Console.WriteLine("[reasoning] end"));
    }

    public IAsyncEnumerable<AgentEvent> OnModelCall(
            Agent agent, RuntimeContext ctx, ModelCallInput input,
            Func<ModelCallInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Console.WriteLine("[model_call] " + input.Model.GetType().Name);
        return IterateWithComplete(next(input), () => Console.WriteLine("[model_call] done"));
    }

    public Task<string> OnSystemPrompt(Agent agent, RuntimeContext ctx, string currentPrompt)
    {
        Console.WriteLine("[system_prompt] length=" + currentPrompt.Length);
        return Task.FromResult(currentPrompt);
    }

    private static async IAsyncEnumerable<AgentEvent> IterateWithComplete(
            IAsyncEnumerable<AgentEvent> source, Action onComplete)
    {
        await foreach (var item in source)
        {
            yield return item;
        }
        onComplete();
    }
}
```

每个 hook 的 input 类型（均位于 `AgentScope.Core.Middleware`）：

| Hook | Input record | 字段 |
|------|--------------|------|
| `OnAgent` | `AgentInput` | `Msgs: List<Msg>` |
| `OnReasoning` | `ReasoningInput` | `Messages: List<Msg>`, `Tools: List<ToolSchema>`, `Options: GenerateOptions` |
| `OnActing` | `ActingInput` | `ToolCalls: List<ToolUseBlock>` |
| `OnModelCall` | `ModelCallInput` | `Messages`, `Tools`, `Options`, `Model: Model` |
| `OnSystemPrompt` | `string` | 当前 prompt |

需要替换流入下一层的字段时，构造一个新的 input record 后再调用 `next.Apply(...)`。

完整可运行示例：`agentscope-examples/documentation/.../middleware/CustomizedMiddlewareExample.java`、`middleware/ModelCallMiddlewareExample.java`、`middleware/SystemPromptMiddlewareExample.java`。

### 读取 RuntimeContext

`IMiddlewareBase` 的所有 hook 都将本次 `CallAsync` / `Stream` 绑定的 [`RuntimeContext`](./agent.md#runtimecontext-per-call-上下文) 作为第二个参数直接传入——既能读会话字段，也能按类型 / 按 key 取属性，还能反向写入来给下游 hook 和 tool 传值。

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;

/// <summary>把 user / request id 打到日志，并把 trace id 写回 context 供 tool 读取。</summary>
public class RequestContextMiddleware : IMiddlewareBase
{
    public IAsyncEnumerable<AgentEvent> OnAgent(
            Agent agent, RuntimeContext ctx, AgentInput input,
            Func<AgentInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Console.WriteLine(
                $"[req] user={ctx.UserId} session={ctx.SessionId} reqId={ctx.Get<string>("request_id")}");
        ctx.Put("trace_id", Guid.NewGuid().ToString());  // 后续 hook / tool 可读
        return next(input);
    }
}
```

注意点：

- 同一份 `RuntimeContext` 在整个 reply 内被各层 hook / tool 共享，使用线程安全的 dictionary，可以安全地 `Put` 写入。
- 不要把请求级状态缓存到 middleware 实例字段——一个 middleware 实例通常被多个 agent / call 复用；要么放进 `RuntimeContext`，要么用 `AsyncLocal`。
- 若 builder 上同时配置了全局 `toolExecutionContext`，框架在分发给 tool 时会把它合并到 per-call context 之后（per-call 优先级更高）。

### 执行顺序

Onion 类 hook（`OnAgent`、`OnReasoning`、`OnActing`、`OnModelCall`）按 `IMiddlewareBase.Order()` 排序——**数值越大越处于最外层**。默认值是 `1`；相同 order 的 middleware 保持其 Builder 注册顺序：

```
middlewares = [mw1(order=2), mw2(order=1)]
// 调用顺序：
// mw1 前 → mw2 前 → 内部逻辑 → mw2 后 → mw1 后
```

自定义 middleware 可覆写 `Order()`，改变其相对默认优先级的位置。例如 order 为 `0` 时，会进入所有仍保持默认 order `1` 的 middleware 内层：

```csharp
IMiddlewareBase lowerPriority = new MiddlewareBaseAdapter
{
    OrderValue = 0,
    OnAgentFunc = (agent, ctx, input, next) => next(input)
};
```

对于流式 / 产出事件的 hook，内层 middleware 先看到每一个 yield 出的事件：

```
mw1_pre → mw2_pre → mw2_event → mw1_event → ... → mw2_post → mw1_post
```

Transformer 类 hook（`OnSystemPrompt`）—— middleware **从左到右串行接力**：

```
middlewares = [mw1, mw2]
// originalPrompt → mw1.OnSystemPrompt() → mw2.OnSystemPrompt() → final
```

一次 reply 中各 hook 的整体执行顺序遵循 agent 生命周期：

```
OnAgent
  └── 每一轮 ReAct：
        ├── OnReasoning
        │     ├── prepare model input → OnSystemPrompt
        │     └── OnModelCall
        └── OnActing（本轮每个工具调用一次）
```

## 实用示例

### 计时 middleware

下面的 middleware 记录每次模型调用的耗时：

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class TimingMiddleware : IMiddlewareBase
{
    public IAsyncEnumerable<AgentEvent> OnModelCall(
            Agent agent, RuntimeContext ctx, ModelCallInput input,
            Func<ModelCallInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Stopwatch sw = Stopwatch.StartNew();
        return IterateWithFinally(next(input), () =>
        {
            sw.Stop();
            Console.WriteLine($"[timing] {agent.Name}: {sw.ElapsedMilliseconds}ms");
        });
    }

    private static async IAsyncEnumerable<AgentEvent> IterateWithFinally(
            IAsyncEnumerable<AgentEvent> source, Action finallyAction)
    {
        try
        {
            await foreach (var item in source)
                yield return item;
        }
        finally
        {
            finallyAction();
        }
    }
}
```

### 限速 middleware

下面的 middleware 在两次模型调用之间强制留出最小间隔：

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class RateLimitMiddleware : IMiddlewareBase
{
    private readonly long _minIntervalMs;
    private long _lastCallTimestamp;

    public RateLimitMiddleware(TimeSpan minInterval)
    {
        _minIntervalMs = (long)minInterval.TotalMilliseconds;
    }

    public IAsyncEnumerable<AgentEvent> OnModelCall(
            Agent agent, RuntimeContext ctx, ModelCallInput input,
            Func<ModelCallInput, IAsyncEnumerable<AgentEvent>> next)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long last = Interlocked.Read(ref _lastCallTimestamp);
        long wait = _minIntervalMs - (now - last);
        if (wait > 0)
        {
            Thread.Sleep((int)wait);
        }
        Interlocked.Exchange(ref _lastCallTimestamp, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        return next(input);
    }
}
```

### 动态 system prompt middleware

下面的 middleware 在 system prompt 中注入实时上下文。也可以直接复用示例 `middleware/SystemPromptMiddlewareExample.cs`：

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Middleware;
using System;
using System.Threading.Tasks;

public class DynamicContextMiddleware : IMiddlewareBase
{
    private readonly Func<string> _contextFn;

    public DynamicContextMiddleware(Func<string> contextFn)
    {
        _contextFn = contextFn;
    }

    public Task<string> OnSystemPrompt(Agent agent, RuntimeContext ctx, string currentPrompt)
    {
        return Task.FromResult(currentPrompt + "\n\n## Current Context\n" + _contextFn());
    }
}

// 装配：
// .WithMiddlewares(new List<IMiddlewareBase> { new DynamicContextMiddleware(() => "Time: " + DateTimeOffset.UtcNow) })
```

### 模型回退 middleware

下面的 middleware 在主模型失败时切换到备用模型：

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;

public class ModelFallbackMiddleware : IMiddlewareBase
{
    private readonly Model _fallback;

    public ModelFallbackMiddleware(Model fallback)
    {
        _fallback = fallback;
    }

    public IAsyncEnumerable<AgentEvent> OnModelCall(
            Agent agent, RuntimeContext ctx, ModelCallInput input,
            Func<ModelCallInput, IAsyncEnumerable<AgentEvent>> next)
    {
        try
        {
            return next(input);
        }
        catch (Exception err)
        {
            Console.Error.WriteLine("Primary model failed: " + err.Message
                    + ", switching to fallback");
            return next(new ModelCallInput(
                    input.Messages,
                    input.Tools,
                    input.Options,
                    _fallback));
        }
    }
}
```

:::{tip}
若只是简单的「主→备」回退，`ReActAgent.Builder` 直接暴露了 `FallbackModel(...)` 与 `MaxRetries(...)`，无需自己写 middleware。
:::

### 全部工具被拒绝时停止 agent

当用户通过 HITL 拒绝了一轮推理产出的全部工具调用时，agent 默认会继续下一轮推理（向后兼容）。如果希望在这种场景下停止 agent，可以编写一个 `OnActing` middleware 观察 `AllToolsDeniedEvent` 并发出 `RequestStopEvent`：

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Message;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;

public class StopOnAllDeniedMiddleware : IMiddlewareBase
{
    public IAsyncEnumerable<AgentEvent> OnActing(
            Agent agent, RuntimeContext ctx, ActingInput input,
            Func<ActingInput, IAsyncEnumerable<AgentEvent>> next)
    {
        return WrapWithStopCheck(next(input));
    }

    private static async IAsyncEnumerable<AgentEvent> WrapWithStopCheck(
            IAsyncEnumerable<AgentEvent> source)
    {
        await foreach (var evt in source)
        {
            if (evt is AllToolsDeniedEvent)
            {
                yield return evt;
                yield return new RequestStopEvent(
                        "All tools denied by user",
                        GenerateReason.AllToolsDenied);
            }
            else
            {
                yield return evt;
            }
        }
    }
}
```

装配后，agent 在所有工具被拒绝时会立即停止，返回 `GenerateReason.AllToolsDenied`：

```csharp
ReActAgent agent =
        ReActAgent.Builder()
                .WithName("guarded")
                .WithSysPrompt("...")
                .WithModel(model)
                .WithToolkit(toolkit)
                .WithMiddlewares(new List<IMiddlewareBase> { new StopOnAllDeniedMiddleware() })
                .Build();
```
