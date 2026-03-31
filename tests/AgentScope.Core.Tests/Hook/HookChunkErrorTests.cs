// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Hook;
using AgentScope.Core.Message;
using Xunit;

namespace AgentScope.Core.Tests.Hook;

public class HookChunkErrorTests
{
    [Fact]
    public async Task ExecuteReasoningChunkHooksAsync_InvokesHook()
    {
        var manager = new HookManager();
        var capturingHook = new ReasoningChunkCaptureHook();
        manager.RegisterHook(capturingHook);
        var ev = new ReasoningChunkEvent { AgentName = "A", Chunk = "hello" };
        await manager.ExecuteReasoningChunkHooksAsync(ev);
        Assert.Equal("hello", capturingHook.LastChunk);
    }

    [Fact]
    public async Task ExecuteActingChunkHooksAsync_InvokesHook()
    {
        var manager = new HookManager();
        var capturingHook = new ActingChunkCaptureHook();
        manager.RegisterHook(capturingHook);
        var ev = new ActingChunkEvent { AgentName = "A", Chunk = "act" };
        await manager.ExecuteActingChunkHooksAsync(ev);
        Assert.Equal("act", capturingHook.LastChunk);
    }

    [Fact]
    public async Task ExecuteSummaryHooksAsync_InvokesHook()
    {
        var manager = new HookManager();
        var capturingHook = new SummaryCaptureHook();
        manager.RegisterHook(capturingHook);

        var preEvent = new PreSummaryEvent { AgentName = "A", SummaryText = "draft" };
        await manager.ExecutePreSummaryHooksAsync(preEvent);

        var chunkEvent = new SummaryChunkEvent { AgentName = "A", Chunk = preEvent.SummaryText };
        await manager.ExecuteSummaryChunkHooksAsync(chunkEvent);

        var postEvent = new PostSummaryEvent { AgentName = "A", SummaryText = preEvent.SummaryText };
        await manager.ExecutePostSummaryHooksAsync(postEvent);

        Assert.Equal("draft-post", capturingHook.LastPreSummary);
        Assert.Equal("draft-post", capturingHook.LastChunk);
        Assert.Equal("draft-post", capturingHook.LastPostSummary);
    }

    [Fact]
    public async Task ExecuteErrorHooksAsync_InvokesHook()
    {
        var manager = new HookManager();
        var capturingHook = new ErrorCaptureHook();
        manager.RegisterHook(capturingHook);
        var ev = new ErrorHookEvent { AgentName = "A", ErrorMessage = "fail" };
        await manager.ExecuteErrorHooksAsync(ev);
        Assert.Equal("fail", capturingHook.LastErrorMessage);
    }

    [Fact]
    public void ErrorHookEvent_WithException_StoresException()
    {
        var ex = new InvalidOperationException("inner");
        var ev = new ErrorHookEvent { ErrorMessage = "outer", Exception = ex };
        Assert.Same(ex, ev.Exception);
        Assert.Equal("outer", ev.ErrorMessage);
    }

    [Fact]
    public async Task ShouldStop_StopsSubsequentHooks()
    {
        var manager = new HookManager();
        var count = 0;
        var stopHook = new StopOnFirstHook(() => count++);
        var secondHook = new StopOnFirstHook(() => count++);
        manager.RegisterHook(stopHook);
        manager.RegisterHook(secondHook);
        var ev = new ReasoningChunkEvent { AgentName = "A", Chunk = "x" };
        await manager.ExecuteReasoningChunkHooksAsync(ev);
        Assert.Equal(1, count);
    }

    private sealed class ReasoningChunkCaptureHook : HookBase
    {
        public string LastChunk { get; private set; } = "";
        public override Task OnReasoningChunkAsync(ReasoningChunkEvent @event)
        {
            LastChunk = @event.Chunk;
            return Task.CompletedTask;
        }
    }

    private sealed class ActingChunkCaptureHook : HookBase
    {
        public string LastChunk { get; private set; } = "";
        public override Task OnActingChunkAsync(ActingChunkEvent @event)
        {
            LastChunk = @event.Chunk;
            return Task.CompletedTask;
        }
    }

    private sealed class ErrorCaptureHook : HookBase
    {
        public string LastErrorMessage { get; private set; } = "";
        public override Task OnErrorAsync(ErrorHookEvent @event)
        {
            LastErrorMessage = @event.ErrorMessage;
            return Task.CompletedTask;
        }
    }

    private sealed class SummaryCaptureHook : HookBase
    {
        public string LastPreSummary { get; private set; } = "";
        public string LastChunk { get; private set; } = "";
        public string LastPostSummary { get; private set; } = "";

        public override Task OnPreSummaryAsync(PreSummaryEvent @event)
        {
            @event.SummaryText += "-post";
            LastPreSummary = @event.SummaryText;
            return Task.CompletedTask;
        }

        public override Task OnSummaryChunkAsync(SummaryChunkEvent @event)
        {
            LastChunk = @event.Chunk;
            return Task.CompletedTask;
        }

        public override Task OnPostSummaryAsync(PostSummaryEvent @event)
        {
            LastPostSummary = @event.SummaryText;
            return Task.CompletedTask;
        }
    }

    private sealed class StopOnFirstHook : HookBase
    {
        private readonly Action _onInvoke;
        public StopOnFirstHook(Action onInvoke) => _onInvoke = onInvoke;
        public override Task OnReasoningChunkAsync(ReasoningChunkEvent @event)
        {
            _onInvoke();
            @event.ShouldStop = true;
            return Task.CompletedTask;
        }
    }
}
