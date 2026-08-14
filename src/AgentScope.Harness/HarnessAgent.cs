using AgentScope.Core;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;
using AgentScope.Core.State;
using AgentScope.Core.Tool;
using AgentScope.Harness.Bus;
using AgentScope.Harness.Filesystem;
using AgentScope.Harness.Gateway;
using AgentScope.Harness.Middleware;
using AgentScope.Harness.Subagent;
using AgentScope.Harness.Team;

namespace AgentScope.Harness;

/// <summary>
/// Harness 主 Agent。对标 Java HarnessAgent。
/// 组合 EnhancedReActAgent + 各子系统（总线/文件系统/团队/网关/中间件），
/// 提供完整的 Agent 运行时环境。
/// </summary>
public sealed class HarnessAgent : IAgent
{
    private readonly EnhancedReActAgent _inner;
    private readonly List<IHarnessMiddleware> _middlewares = [];
    private readonly IMessageBus _bus;
    private readonly IFilesystem _filesystem;
    private readonly IGateway _gateway;

    public string AgentId => _inner.AgentId;
    public string Name => _inner.Name;
    public string Description => _inner.Description;

    internal HarnessAgent(
        EnhancedReActAgent inner,
        IMessageBus bus,
        IFilesystem filesystem,
        IGateway gateway,
        IEnumerable<IHarnessMiddleware>? middlewares = null)
    {
        _inner = inner;
        _bus = bus;
        _filesystem = filesystem;
        _gateway = gateway;
        if (middlewares != null) _middlewares.AddRange(middlewares);
    }

    public Task<Msg> CallAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
        => ExecuteWithMiddlewareAsync(messages, () => _inner.CallAsync(messages, context), context);

    public Task<Msg> CallAsync(Msg message, RuntimeContext? context = null)
        => CallAsync([message], context);

    public Task<Msg> CallAsync(string text, RuntimeContext? context = null)
    {
        var msg = Msg.Builder().Role("user").TextContent(text).Build();
        return CallAsync([msg], context);
    }

    public IAsyncEnumerable<Event> StreamEventsAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null)
        => _inner.StreamEventsAsync(messages, context);

    public IAsyncEnumerable<Event> StreamEventsAsync(Msg message, RuntimeContext? context = null)
        => _inner.StreamEventsAsync(message, context);

    public Task ObserveAsync(Msg message, RuntimeContext? context = null) => CallAsync(message, context);
    public Task ObserveAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null) => CallAsync(messages, context);
    public void Interrupt() => _inner.Interrupt();
    public void Interrupt(Msg message) => _inner.Interrupt(message);

    private async Task<Msg> ExecuteWithMiddlewareAsync(IReadOnlyList<Msg> messages,
        Func<Task<Msg>> coreFn, RuntimeContext? context)
    {
        var mctx = new MiddlewareContext
        {
            AgentName = Name,
            Runtime = context,
            Messages = messages.ToList()
        };
        mctx.Items["filesystem"] = _filesystem;
        mctx.Items["bus"] = _bus;
        mctx.Items["session_id"] = context?.SessionId ?? "default";

        // 按 Order 排序执行中间件链
        var sorted = _middlewares.OrderBy(m => m.Order).ToList();
        if (sorted.Count == 0) return await coreFn().ConfigureAwait(false);

        // 系统提示词拦截链：依次让每个中间件改写提示词，最终写回内层 Agent。
        var prompt = _inner.SystemPrompt;
        foreach (var mw in sorted)
        {
            try
            {
                prompt = await mw.OnSystemPromptAsync(mctx, prompt).ConfigureAwait(false);
            }
            catch
            {
                // 提示词注入失败不得中断主流程
            }
        }
        _inner.SystemPrompt = prompt;

        // 洋葱模型：每个中间件真正包裹核心调用，因此可以在 next() 前后做事，
        // 也可以选择不调用 next() 来短路整个回合。
        Msg? result = null;
        var coreInvoked = false;

        async ValueTask RunChain(int index)
        {
            if (index >= sorted.Count)
            {
                coreInvoked = true;
                result = await coreFn().ConfigureAwait(false);
                return;
            }
            await sorted[index].OnAgentAsync(mctx, () => RunChain(index + 1)).ConfigureAwait(false);
        }

        await RunChain(0).ConfigureAwait(false);

        // 有中间件短路了链条：回退为直接执行核心，保持既有调用语义不被破坏。
        if (!coreInvoked) result = await coreFn().ConfigureAwait(false);

        return result!;
    }
}
