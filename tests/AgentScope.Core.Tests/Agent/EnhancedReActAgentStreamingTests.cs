// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Accumulator;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Hook;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using Xunit;
using AgentEvent = AgentScope.Core.Events.Event;
using AgentEventType = AgentScope.Core.Events.EventType;

namespace AgentScope.Core.Tests.Agent;

/// <summary>
/// Tests for EnhancedReActAgent streaming behavior, hooks, and event emission
/// EnhancedReActAgent 流式行为、钩子和事件发射测试
/// </summary>
public class EnhancedReActAgentStreamingTests
{
    /// <summary>
    /// Tests that StreamAsync with a streaming model emits reasoning/summary events and a final response.
    /// 测试流式模型下 StreamAsync 发射推理/摘要事件并返回最终响应。
    /// </summary>
    [Fact]
    public async Task StreamAsync_WithStreamingModel_EmitsSummaryEvents_And_FinalResponse()
    {
        var model = new StreamingScriptedModel(
            "Thought: 先分析问题",
            "\nAction: finish",
            "\nAction Input: 你好！");

        var agent = EnhancedReActAgent.Builder()
            .Name("StreamAgent")
            .Model(model)
            .MaxIterations(2)
            .Build();

        var userMessage = Msg.Builder().Role("user").TextContent("你好").Build();
        var events = await CollectEventsAsync(agent.StreamAsync(userMessage));

        Assert.NotEmpty(events);
        Assert.Equal(AgentEventType.ReasoningStart, events[0].Type);
        Assert.Equal(3, events.Count(e => e.Type == AgentEventType.ReasoningChunk));
        Assert.Contains(events, e => e.Type == AgentEventType.SummaryStart);
        Assert.Contains(events, e => e.Type == AgentEventType.SummaryChunk && e.Message?.GetTextContent() == "你好！");
        Assert.Contains(events, e => e.Type == AgentEventType.ActingFinish && !e.IsLast);
        Assert.Equal(AgentEventType.SummaryFinish, events[^1].Type);
        Assert.True(events[^1].IsLast);
        Assert.Equal("你好！", events[^1].Message?.GetTextContent());
    }

    /// <summary>
    /// Tests that CallAsync with a non-streaming model still invokes reasoning and summary chunk hooks.
    /// 测试非流式模型下 CallAsync 仍会触发推理和摘要块钩子。
    /// </summary>
    [Fact]
    public async Task CallAsync_WithNonStreamingModel_InvokesReasoningAndSummaryChunkHooks()
    {
        var manager = new HookManager();
        var hook = new CaptureHook();
        manager.RegisterHook(hook);

        var model = new ScriptedModel("Thought: 需要直接回复\nAction: finish\nAction Input: 最终答复");
        var agent = EnhancedReActAgent.Builder()
            .Name("HookAgent")
            .Model(model)
            .HookManager(manager)
            .MaxIterations(2)
            .Build();

        var response = await agent.CallAsync(Msg.Builder().Role("user").TextContent("测试").Build());

        Assert.Equal("最终答复", response.GetTextContent());
        Assert.Contains(hook.ReasoningChunks, chunk => chunk.Contains("Thought: 需要直接回复", StringComparison.Ordinal));
        Assert.Empty(hook.ActingChunks);
        Assert.Contains("最终答复", hook.SummaryChunks);
        Assert.Empty(hook.Errors);
    }

    /// <summary>
    /// Tests that when the model fails, the error hook is invoked and the error message appears in the response.
    /// 测试模型失败时错误钩子被触发且错误信息出现在响应中。
    /// </summary>
    [Fact]
    public async Task CallAsync_WhenModelFails_InvokesErrorHook()
    {
        var manager = new HookManager();
        var hook = new CaptureHook();
        manager.RegisterHook(hook);

        var agent = EnhancedReActAgent.Builder()
            .Name("ErrorAgent")
            .Model(new FailingModel("boom"))
            .HookManager(manager)
            .Build();

        var response = await agent.CallAsync(Msg.Builder().Role("user").TextContent("测试").Build());
        var responseText = response.GetTextContent();

        Assert.NotNull(responseText);
        Assert.Contains("boom", responseText);
        Assert.Contains(hook.Errors, error => error.Contains("boom", StringComparison.Ordinal));
    }

    /// <summary>
    /// Tests that AgentStreamAdapter properly delegates to the inner agent's streaming method.
    /// 测试 AgentStreamAdapter 正确委托给内部代理的流式方法。
    /// </summary>
    [Fact]
    public async Task AgentStreamAdapter_WithStreamableAgent_DelegatesToInnerStream()
    {
        var model = new StreamingScriptedModel(
            "Thought: 代理适配",
            "\nAction: finish",
            "\nAction Input: 适配完成");
        var innerAgent = EnhancedReActAgent.Builder()
            .Name("InnerStreamAgent")
            .Model(model)
            .Build();

        var adapter = new AgentStreamAdapter(innerAgent);
        var events = await CollectEventsAsync(adapter.StreamAsync(Msg.Builder().Role("user").TextContent("hello").Build()));

        Assert.Contains(events, e => e.Type == AgentEventType.ReasoningChunk);
        Assert.Equal(AgentEventType.SummaryFinish, events[^1].Type);
        Assert.Equal("适配完成", events[^1].Message?.GetTextContent());
    }

    /// <summary>
    /// Tests that StreamAsync with an accumulating hook builds a ReasoningContext from chunk events.
    /// 测试使用累加钩子时 StreamAsync 从块事件构建 ReasoningContext。
    /// </summary>
    [Fact]
    public async Task StreamAsync_WithAccumulatingHook_BuildsReasoningContextFromChunks()
    {
        var manager = new HookManager();
        var hook = new AccumulatingHook();
        manager.RegisterHook(hook);

        var model = new StreamingScriptedModel(
            "Thought: 先分析问题",
            "\nAction: finish",
            "\nAction Input: 汇总结果");

        var agent = EnhancedReActAgent.Builder()
            .Name("AccumulatingAgent")
            .Model(model)
            .HookManager(manager)
            .Build();

        var events = await CollectEventsAsync(agent.StreamAsync(Msg.Builder().Role("user").TextContent("测试").Build()));

        Assert.Contains(events, e => e.Type == AgentEventType.SummaryChunk);
        Assert.Contains("Thought: 先分析问题", hook.Context.AccumulatedThinking, StringComparison.Ordinal);
        Assert.Equal("汇总结果", hook.Context.AccumulatedText);

        var finalMessage = hook.Context.BuildFinalMessage();
        Assert.Equal("assistant", finalMessage.Role);
    }

    /// <summary>
    /// Collects all events from an async enumerable stream into a list.
    /// 从异步可枚举流中收集所有事件到列表中。
    /// </summary>
    private static async Task<List<AgentEvent>> CollectEventsAsync(IAsyncEnumerable<AgentEvent> stream)
    {
        var events = new List<AgentEvent>();
        await foreach (var item in stream)
        {
            events.Add(item);
        }

        return events;
    }

    /// <summary>
    /// A hook that captures reasoning, acting, summary chunks and errors for assertions.
    /// 捕获推理、行动、摘要块和错误用于断言的测试钩子。
    /// </summary>
    private sealed class CaptureHook : HookBase
    {
        public List<string> ReasoningChunks { get; } = new();
        public List<string> ActingChunks { get; } = new();
        public List<string> SummaryChunks { get; } = new();
        public List<string> Errors { get; } = new();

        public override Task OnReasoningChunkAsync(ReasoningChunkEvent @event)
        {
            ReasoningChunks.Add(@event.Chunk);
            return Task.CompletedTask;
        }

        public override Task OnActingChunkAsync(ActingChunkEvent @event)
        {
            ActingChunks.Add(@event.Chunk);
            return Task.CompletedTask;
        }

        public override Task OnSummaryChunkAsync(SummaryChunkEvent @event)
        {
            SummaryChunks.Add(@event.Chunk);
            return Task.CompletedTask;
        }

        public override Task OnErrorAsync(ErrorHookEvent @event)
        {
            Errors.Add(@event.ErrorMessage);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A hook that accumulates reasoning and summary chunks into a ReasoningContext.
    /// 将推理和摘要块累加到 ReasoningContext 的测试钩子。
    /// </summary>
    private sealed class AccumulatingHook : HookBase
    {
        public ReasoningContext Context { get; } = new();

        public override Task OnReasoningChunkAsync(ReasoningChunkEvent @event)
        {
            Context.ProcessChunk(new ThinkingBlock { Thinking = @event.Chunk });
            return Task.CompletedTask;
        }

        public override Task OnSummaryChunkAsync(SummaryChunkEvent @event)
        {
            Context.ProcessChunk(new TextBlock { Text = @event.Chunk });
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// A scripted model that returns predefined text responses in queue order for deterministic testing.
    /// 按队列顺序返回预定义文本响应的脚本化模型，用于确定性测试。
    /// </summary>
    private sealed class ScriptedModel : IModel
    {
        private readonly Queue<string> _responses;

        public ScriptedModel(params string[] responses)
        {
            _responses = new Queue<string>(responses);
        }

        public string ModelName => "scripted";

        public IObservable<ModelResponse> Generate(ModelRequest request)
        {
            return Observable.Return(CreateResponse());
        }

        public Task<ModelResponse> GenerateAsync(ModelRequest request)
        {
            return Task.FromResult(CreateResponse());
        }

        private ModelResponse CreateResponse()
        {
            var text = _responses.Count > 0 ? _responses.Dequeue() : string.Empty;
            return new ModelResponse
            {
                Success = true,
                Text = text
            };
        }
    }

    /// <summary>
    /// A streaming scripted model that yields predefined chunks via GenerateStreamAsync for testing streaming agents.
    /// 通过 GenerateStreamAsync 按块产出预定义片段的流式脚本化模型，用于测试流式代理。
    /// </summary>
    private sealed class StreamingScriptedModel : IModel, IStreamingChatModel
    {
        private readonly IReadOnlyList<string> _chunks;

        public StreamingScriptedModel(params string[] chunks)
        {
            _chunks = chunks;
        }

        public string ModelName => "streaming-scripted";

        public IObservable<ModelResponse> Generate(ModelRequest request)
        {
            return Observable.Return(CreateFullResponse());
        }

        public Task<ModelResponse> GenerateAsync(ModelRequest request)
        {
            return Task.FromResult(CreateFullResponse());
        }

        public async IAsyncEnumerable<ChatResponse> GenerateStreamAsync(
            List<Msg> messages,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            _ = messages;
            foreach (var chunk in _chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return new ChatResponse
                {
                    Success = true,
                    Text = chunk,
                    Content = chunk,
                    Model = ModelName,
                    IsComplete = false
                };
            }
        }

        private ModelResponse CreateFullResponse()
        {
            return new ModelResponse
            {
                Success = true,
                Text = string.Concat(_chunks)
            };
        }
    }

    /// <summary>
    /// A model that always returns a failure response with a configured error message.
    /// 始终返回失败响应并携带指定错误信息的测试模型。
    /// </summary>
    private sealed class FailingModel : IModel
    {
        private readonly string _error;

        public FailingModel(string error)
        {
            _error = error;
        }

        public string ModelName => "failing";

        public IObservable<ModelResponse> Generate(ModelRequest request)
        {
            _ = request;
            return Observable.Return(CreateResponse());
        }

        public Task<ModelResponse> GenerateAsync(ModelRequest request)
        {
            _ = request;
            return Task.FromResult(CreateResponse());
        }

        private ModelResponse CreateResponse()
        {
            return new ModelResponse
            {
                Success = false,
                Error = _error
            };
        }
    }
}