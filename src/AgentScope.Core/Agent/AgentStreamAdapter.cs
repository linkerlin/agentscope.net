// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// 将任意 IAgent 桥接为事件流：在不修改原有 IAgent 的前提下，通过 CallAsync 得到结果后产出事件序列。
/// 满足「旧 API 全通过；新适配层可消费并输出事件」。
/// </summary>
public sealed class AgentStreamAdapter : IStreamableAgent
{
    private readonly IAgent _inner;

    public AgentStreamAdapter(IAgent inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public string Name => _inner.Name;

    public IObservable<Msg> Call(Msg message) => _inner.Call(message);

    public Task<Msg> CallAsync(Msg message) => _inner.CallAsync(message);

    public async IAsyncEnumerable<Event> StreamAsync(IEnumerable<Msg> messages, StreamOptions options)
    {
        options.CancellationToken.ThrowIfCancellationRequested();
        var list = messages as IList<Msg> ?? messages.ToList();
        if (list.Count == 0)
        {
            yield return new Event(EventType.ReasoningFinish, null, true);
            yield break;
        }
        var lastInput = list[list.Count - 1];
        Msg? response = null;
        string? errorMessage = null;
        try
        {
            response = await _inner.CallAsync(lastInput).ConfigureAwait(false);
        }
        catch (System.Exception ex)
        {
            errorMessage = ex.Message;
        }
        if (errorMessage != null)
        {
            yield return Event.ErrorEvent(null, errorMessage, isLast: true);
            yield break;
        }
        if (options.IncludeReasoning)
            yield return new Event(EventType.ReasoningStart, null, false);
        if (options.IncludeToolCalls)
            yield return new Event(EventType.ActingStart, null, false);
        yield return new Event(EventType.ActingFinish, response!, isLast: true);
    }

    public async IAsyncEnumerable<Event> StreamAsync(Msg message, StreamOptions? options = null)
    {
        options ??= new StreamOptions();
        await foreach (var ev in StreamAsync(new[] { message }, options).ConfigureAwait(false))
            yield return ev;
    }
}
