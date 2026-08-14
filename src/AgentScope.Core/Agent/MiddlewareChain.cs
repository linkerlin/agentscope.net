using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AgentScope.Core.Events;

namespace AgentScope.Core.Agent;

/// <summary>
/// 中间件链，将多个 MiddlewareBase 按注册顺序反向链接为调用链
/// 对应 Java: io.agentscope.core.middleware.MiddlewareChain
/// </summary>
public sealed class MiddlewareChain
{
    private readonly List<MiddlewareBase> _middlewares = [];

    public MiddlewareChain Add(MiddlewareBase mw)
    {
        _middlewares.Add(mw);
        return this;
    }

    public MiddlewareChain AddRange(IEnumerable<MiddlewareBase> mws)
    {
        _middlewares.AddRange(mws);
        return this;
    }

    public IReadOnlyList<MiddlewareBase> Middlewares => _middlewares;

    /// <summary>构建 Agent 主调用链</summary>
    public Func<AgentInput, IAsyncEnumerable<Event>> BuildAgentChain(
        Func<AgentInput, IAsyncEnumerable<Event>> coreHandler)
    {
        Func<AgentInput, IAsyncEnumerable<Event>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = input => mw.OnAgentAsync(input, next);
        }
        return chain;
    }

    /// <summary>构建推理阶段链</summary>
    public Func<ReasoningInput, Task<ReasoningInput>> BuildReasoningChain(
        Func<ReasoningInput, Task<ReasoningInput>> coreHandler)
    {
        Func<ReasoningInput, Task<ReasoningInput>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = input => mw.OnReasoningAsync(input, next);
        }
        return chain;
    }

    /// <summary>构建行动阶段链</summary>
    public Func<ActingInput, Task<ActingInput>> BuildActingChain(
        Func<ActingInput, Task<ActingInput>> coreHandler)
    {
        Func<ActingInput, Task<ActingInput>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = input => mw.OnActingAsync(input, next);
        }
        return chain;
    }

    /// <summary>构建模型调用链</summary>
    public Func<ModelCallInput, Task<ModelCallInput>> BuildModelCallChain(
        Func<ModelCallInput, Task<ModelCallInput>> coreHandler)
    {
        Func<ModelCallInput, Task<ModelCallInput>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = input => mw.OnModelCallAsync(input, next);
        }
        return chain;
    }

    /// <summary>构建系统提示词链</summary>
    public Func<IAgent, RuntimeContext, string, Task<string>> BuildSystemPromptChain(
        Func<IAgent, RuntimeContext, string, Task<string>> coreHandler)
    {
        Func<IAgent, RuntimeContext, string, Task<string>> chain = coreHandler;
        for (var i = _middlewares.Count - 1; i >= 0; i--)
        {
            var mw = _middlewares[i];
            var next = chain;
            chain = (agent, ctx, prompt) => mw.OnSystemPromptAsync(agent, ctx, prompt)
                .ContinueWith(t => next(agent, ctx, t.Result)).Unwrap();
        }
        return chain;
    }
}
