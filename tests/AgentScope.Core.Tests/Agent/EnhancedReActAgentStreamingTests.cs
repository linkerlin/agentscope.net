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

public class EnhancedReActAgentStreamingTests
{
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

    private static async Task<List<AgentEvent>> CollectEventsAsync(IAsyncEnumerable<AgentEvent> stream)
    {
        var events = new List<AgentEvent>();
        await foreach (var item in stream)
        {
            events.Add(item);
        }

        return events;
    }

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