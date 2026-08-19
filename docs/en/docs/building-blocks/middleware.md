---
title: "Middleware"
description: "Intercept and extend agent behavior at key lifecycle points"
---

## Overview

Agent middleware lets you inject custom logic (logging, tracing, input rewriting, access control, …) at key points in an agent's execution flow without modifying the agent or model code.

In AgentScope .NET, you can hook into 5 places — covering everything from the outer reply flow down to the raw model API call:

| Position | Type | Description |
|----------|------|-------------|
| `OnAgent` | Onion | Wraps a full reply flow, covering all ReAct rounds, tool execution, and the final output |
| `OnReasoning` | Onion | Wraps one reasoning step in the ReAct loop (input assembly → model call → streaming decode) |
| `OnActing` | Onion | Wraps the execution of a single tool call |
| `OnModelCall` | Onion | Wraps a raw `ChatModel` API call — closest to the model |
| `OnSystemPrompt` | Transformer | Triggers when the system prompt is assembled; multiple middlewares run in sequence, each transforming the previous output |

The two types differ:

- **Onion** — middleware wraps the next handler; you can insert logic before/after `next(input)` and observe the intermediate event stream.
- **Transformer** — middlewares form a pipeline; the previous output is the next input. There's no "inner layer" concept.

The diagram below shows how the hooks nest in the agent lifecycle. `OnSystemPrompt` is nested inside `OnReasoning` because it fires when the reasoning step assembles the system prompt:

```text
OnAgent/
└── ReAct loop (per round)/
    ├── OnReasoning/
    │   ├── OnSystemPrompt (assemble system prompt)
    │   └── OnModelCall (model API call)
    └── OnActing (per tool call)
```

:::{note}
`OnActing` only wraps tool executions inside the agent runtime. Tools executed outside the agent via external execution are not tracked by `OnActing`.
:::

## Equipping middleware

AgentScope packs a set of hooks into a single `MiddlewareBase` implementation — one middleware class can implement any subset of the 5 hooks (the rest default to `next(input)`). Pass the instances to the builder's `Middlewares(...)`:

```csharp
using AgentScope.Core;
using AgentScope.Core.Middleware;
using AgentScope.Core.Tracing;
using System.Collections.Generic;

ReActAgent agent =
        ReActAgent.Builder()
                .Name("assistant")
                .SysPrompt("You are a helpful assistant.")
                .Model(model)
                .Toolkit(toolkit)
                .Middlewares(new List<MiddlewareBase> { new OtelTracingMiddleware() })
                .Build();
```

`Middleware(...)` (singular) adds one; `Middlewares(...)` accepts `List<MiddlewareBase>`. Hooks not implemented by a middleware are skipped at zero cost.

## Built-in middlewares

### OtelTracingMiddleware

`OtelTracingMiddleware` (`AgentScope.Core.Tracing`) wires up [OpenTelemetry](https://opentelemetry.io/docs/specs/semconv/gen-ai/) tracing for the agent lifecycle. It instruments `OnAgent`, `OnModelCall`, `OnActing`, producing nested spans:

- `invoke_agent <name>` — wraps a full reply
- `chat <model>` — wraps each model API call
- `execute_tool <name>` — wraps each tool execution

When no OpenTelemetry SDK is configured (only the default no-op provider), every hook short-circuits to `next(input)` — near-zero overhead.

Initialise the OpenTelemetry SDK in your process (OTLP exporter, `SdkTracerProvider`, `OpenTelemetrySdkBuilder`) and then equip the middleware:

```csharp
using AgentScope.Core;
using AgentScope.Core.Tracing;
using System.Collections.Generic;

ReActAgent agent =
        ReActAgent.Builder()
                .Name("assistant")
                .SysPrompt("You are a helpful assistant.")
                .Model(model)
                .Toolkit(toolkit)
                .Middlewares(new List<MiddlewareBase> { new OtelTracingMiddleware() })
                .Build();
```

Each reply produces a nested span tree with attributes such as agent name, session ID, model name, token counts, tool name, and inputs.

### TaskReminderMiddleware

`TaskReminderMiddleware` (`AgentScope.Core.Middleware`) pairs with the built-in `TodoTools`: before every reasoning step it renders the current `AgentState.TasksContext` as a `<system-reminder>` and injects it into the context, keeping long-running tasks aligned with the plan.

Enable it together with `TodoTools` via `EnableTaskList(true)`:

```csharp
using AgentScope.Core;
using AgentScope.Core.Tool;
using AgentScope.Core.Tool.Builtin;

Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new TodoTools());

ReActAgent agent =
        ReActAgent.Builder()
                .Name("planner")
                .SysPrompt("You plan tasks step by step.")
                .Model(model)
                .Toolkit(toolkit)
                .EnableTaskList(true)
                .Build();
```

## Custom middleware

Implement `MiddlewareBase` (`AgentScope.Core.Middleware`) and override only the hooks you need.

Each onion hook receives a `next` function — calling `next(input)` enters the next layer. You can insert logic before or after, or use LINQ operators to observe and rewrite the event stream.

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;

/// Observes agent / reasoning / model_call / system_prompt at the same time.
public class FullObservabilityMiddleware : MiddlewareBase
{
    public override IAsyncEnumerable<AgentEvent> OnAgent(
            IAgent agent, RuntimeContext ctx, AgentInput input, Func<AgentInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Console.WriteLine("[agent] start for " + agent.GetName());
        return WrapWithComplete(next(input), () => Console.WriteLine("[agent] end for " + agent.GetName()));
    }

    public override IAsyncEnumerable<AgentEvent> OnReasoning(
            IAgent agent, RuntimeContext ctx, ReasoningInput input, Func<ReasoningInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Console.WriteLine("[reasoning] start");
        return WrapWithComplete(next(input), () => Console.WriteLine("[reasoning] end"));
    }

    public override IAsyncEnumerable<AgentEvent> OnModelCall(
            IAgent agent, RuntimeContext ctx, ModelCallInput input, Func<ModelCallInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Console.WriteLine("[model_call] " + input.Model.GetType().Name);
        return WrapWithComplete(next(input), () => Console.WriteLine("[model_call] done"));
    }

    public override string OnSystemPrompt(IAgent agent, RuntimeContext ctx, string currentPrompt)
    {
        Console.WriteLine("[system_prompt] length=" + currentPrompt.Length);
        return currentPrompt;
    }

    private static async IAsyncEnumerable<AgentEvent> WrapWithComplete(
            IAsyncEnumerable<AgentEvent> inner, Action onComplete)
    {
        await foreach (var e in inner)
            yield return e;
        onComplete();
    }
}
```

Input record types per hook (under `AgentScope.Core.Middleware`):

| Hook | Input record | Fields |
|------|--------------|--------|
| `OnAgent` | `AgentInput` | `Msgs: List<Msg>` |
| `OnReasoning` | `ReasoningInput` | `Messages: List<Msg>`, `Tools: List<ToolSchema>`, `Options: GenerateOptions` |
| `OnActing` | `ActingInput` | `ToolCalls: List<ToolUseBlock>` |
| `OnModelCall` | `ModelCallInput` | `Messages`, `Tools`, `Options`, `Model: IModel` |
| `OnSystemPrompt` | `string` | The current prompt |

To replace fields flowing into the next layer, construct a new input record, then call `next(...)`.

Runnable examples: `agentscope-examples/documentation/.../middleware/CustomizedMiddlewareExample.cs`, `middleware/ModelCallMiddlewareExample.cs`, `middleware/SystemPromptMiddlewareExample.cs`.

### Reading RuntimeContext

Every `MiddlewareBase` hook receives the [`RuntimeContext`](./agent.md#runtimecontext-per-call-context) bound for this `CallAsync` / `Stream` as the second argument — you can read session fields and typed/string attributes, and you can write back to it to forward values to downstream hooks and tools.

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;

/// Log user / request id and propagate a trace id for downstream tools.
public class RequestContextMiddleware : MiddlewareBase
{
    public override IAsyncEnumerable<AgentEvent> OnAgent(
            IAgent agent, RuntimeContext ctx, AgentInput input, Func<AgentInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Console.WriteLine(
                "[req] user={0} session={1} reqId={2}",
                ctx.GetUserId(),
                ctx.GetSessionId(),
                ctx.Get("request_id"));
        ctx.Put("trace_id", Guid.NewGuid().ToString());  // visible to later hooks / tools
        return next(input);
    }
}
```

Things to keep in mind:

- The same `RuntimeContext` instance is shared by every hook and tool in the reply; its maps are thread-safe, so `Put` from any hook is safe.
- Don't cache per-request state on middleware instance fields — a middleware instance is typically reused across agents / calls. Use `RuntimeContext` instead.
- If the builder also has a global `ToolExecutionContext`, the framework merges it after the per-call context when dispatching to tools (per-call wins on key collisions).

### Execution order

Onion hooks (`OnAgent`, `OnReasoning`, `OnActing`, `OnModelCall`) are ordered by `MiddlewareBase.Order()` — **higher values are outermost**. The default order is `1`; middlewares with the same order retain their builder registration order:

```
middlewares = [mw1(order=2), mw2(order=1)]
// Order:
// mw1 pre → mw2 pre → inner → mw2 post → mw1 post
```

Override `Order()` to move a custom middleware relative to the default order. For example, an order of `0` runs inside middleware that keeps the default order of `1`:

```csharp
MiddlewareBase lowerPriority = new MiddlewareBaseImpl();

public class MiddlewareBaseImpl : MiddlewareBase
{
    public override int Order() => 0;
}
```

For streaming / event-emitting hooks, the inner middleware sees each emitted event first:

```
mw1_pre → mw2_pre → mw2_event → mw1_event → ... → mw2_post → mw1_post
```

Transformer hooks (`OnSystemPrompt`) — **left to right pipeline**:

```
middlewares = [mw1, mw2]
// originalPrompt → mw1.OnSystemPrompt() → mw2.OnSystemPrompt() → final
```

Overall hook execution order across one reply:

```
OnAgent
  └── per ReAct round:
        ├── OnReasoning
        │     ├── prepare model input → OnSystemPrompt
        │     └── OnModelCall
        └── OnActing (per tool call)
```

## Practical examples

### Timing middleware

The middleware below records the wall-clock time of each model call:

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;
using System.Diagnostics;

public class TimingMiddleware : MiddlewareBase
{
    public override IAsyncEnumerable<AgentEvent> OnModelCall(
            IAgent agent, RuntimeContext ctx, ModelCallInput input, Func<ModelCallInput, IAsyncEnumerable<AgentEvent>> next)
    {
        Stopwatch sw = Stopwatch.StartNew();
        return WrapWithTiming(next(input), sw, agent);
    }

    private static async IAsyncEnumerable<AgentEvent> WrapWithTiming(
            IAsyncEnumerable<AgentEvent> inner, Stopwatch sw, IAgent agent)
    {
        await foreach (var e in inner)
            yield return e;
        sw.Stop();
        Console.WriteLine("[timing] " + agent.GetName() + ": " + sw.ElapsedMilliseconds + "ms");
    }
}
```

### Rate-limit middleware

Enforce a minimum interval between two model calls:

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class RateLimitMiddleware : MiddlewareBase
{
    private readonly long _minIntervalMs;
    private long _lastCall;

    public RateLimitMiddleware(TimeSpan minInterval)
    {
        _minIntervalMs = (long)minInterval.TotalMilliseconds;
    }

    public override async IAsyncEnumerable<AgentEvent> OnModelCall(
            IAgent agent, RuntimeContext ctx, ModelCallInput input, Func<ModelCallInput, IAsyncEnumerable<AgentEvent>> next)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long wait = _minIntervalMs - (now - Interlocked.Read(ref _lastCall));

        if (wait > 0)
            await Task.Delay(TimeSpan.FromMilliseconds(wait));

        Interlocked.Exchange(ref _lastCall, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        await foreach (var e in next(input))
            yield return e;
    }
}
```

### Dynamic system-prompt middleware

Inject runtime context into the system prompt:

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Middleware;
using System;

public class DynamicContextMiddleware : MiddlewareBase
{
    private readonly Func<string> _contextFn;

    public DynamicContextMiddleware(Func<string> contextFn)
    {
        _contextFn = contextFn;
    }

    public override string OnSystemPrompt(IAgent agent, RuntimeContext ctx, string currentPrompt)
    {
        return currentPrompt + "\n\n## Current Context\n" + _contextFn();
    }
}

// Wire-up:
// .Middlewares(new List<MiddlewareBase> { new DynamicContextMiddleware(() => "Time: " + DateTime.Now) })
```

### Model-fallback middleware

Swap to a backup model if the primary fails:

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Middleware;
using AgentScope.Core.Model;
using System;
using System.Collections.Generic;

public class ModelFallbackMiddleware : MiddlewareBase
{
    private readonly IModel _fallback;

    public ModelFallbackMiddleware(IModel fallback)
    {
        _fallback = fallback;
    }

    public override async IAsyncEnumerable<AgentEvent> OnModelCall(
            IAgent agent, RuntimeContext ctx, ModelCallInput input, Func<ModelCallInput, IAsyncEnumerable<AgentEvent>> next)
    {
        IAsyncEnumerable<AgentEvent> result;
        try
        {
            await foreach (var e in next(input))
                yield return e;
        }
        catch (Exception err)
        {
            Console.Error.WriteLine("Primary model failed: " + err.Message
                    + ", switching to fallback");
            await foreach (var e in next(new ModelCallInput(
                    input.Messages, input.Tools, input.Options, _fallback)))
                yield return e;
        }
    }
}
```

:::{tip}
For a simple primary→backup fallback, `ReActAgent.Builder` already exposes `FallbackModel(...)` and `MaxRetries(...)` directly — no middleware needed.
:::

### Stop agent when all tools are denied

When a user denies all tool calls from a reasoning step via HITL, the agent continues to the next reasoning iteration by default (backward compatible). To stop the agent in this scenario, write an `OnActing` middleware that observes `AllToolsDeniedEvent` and emits a `RequestStopEvent`:

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Event;
using AgentScope.Core.Message;
using AgentScope.Core.Middleware;
using System;
using System.Collections.Generic;

public class StopOnAllDeniedMiddleware : MiddlewareBase
{
    public override async IAsyncEnumerable<AgentEvent> OnActing(
            IAgent agent, RuntimeContext ctx, ActingInput input,
            Func<ActingInput, IAsyncEnumerable<AgentEvent>> next)
    {
        await foreach (var evt in next(input))
        {
            if (evt is AllToolsDeniedEvent)
            {
                yield return evt;
                yield return new RequestStopEvent(
                        "All tools denied by user",
                        GenerateReason.ALL_TOOLS_DENIED);
            }
            else
            {
                yield return evt;
            }
        }
    }
}
```

Once wired up, the agent stops immediately when all tools are denied, returning `GenerateReason.ALL_TOOLS_DENIED`:

```csharp
ReActAgent agent =
        ReActAgent.Builder()
                .Name("guarded")
                .SysPrompt("...")
                .Model(model)
                .Toolkit(toolkit)
                .Middlewares(new List<MiddlewareBase> { new StopOnAllDeniedMiddleware() })
                .Build();
```
