// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Accumulator;
using AgentScope.Core.Message;
using Xunit;

namespace AgentScope.Core.Tests.Accumulator;

public class AccumulatorTests
{
    [Fact]
    public void TextAccumulator_AccumulatesAndReturnsSingleBlock()
    {
        var acc = new TextAccumulator();
        acc.Accumulate(new TextBlock { Text = "a" });
        acc.Accumulate(new TextBlock { Text = "b" });
        var block = acc.GetAccumulated() as TextBlock;
        Assert.NotNull(block);
        Assert.Equal("ab", block!.Text);
        Assert.Equal("ab", acc.GetText());
        acc.Reset();
        Assert.Null(acc.GetAccumulated());
    }

    [Fact]
    public void ThinkingAccumulator_AccumulatesThinkingBlocks()
    {
        var acc = new ThinkingAccumulator();
        acc.Accumulate(new ThinkingBlock { Thinking = "think1" });
        acc.Accumulate(new ThinkingBlock { Thinking = "think2" });
        Assert.Equal("think1think2", acc.GetThinking());
        var block = acc.GetAccumulated() as ThinkingBlock;
        Assert.NotNull(block);
        Assert.Equal("think1think2", block!.Thinking);
    }

    [Fact]
    public void ToolCallsAccumulator_CollectsToolBlocks()
    {
        var acc = new ToolCallsAccumulator();
        acc.Accumulate(new ToolUseBlock { Id = "1", Name = "tool_a", Input = new Dictionary<string, object>() });
        acc.Accumulate(new ToolResultBlock { Id = "1", Output = "ok" });
        var blocks = acc.GetBlocks();
        Assert.Equal(2, blocks.Count);
        Assert.Equal("tool_use", blocks[0].Type);
        Assert.Equal("tool_result", blocks[1].Type);
        Assert.Same(blocks[1], acc.GetAccumulated());
    }

    [Fact]
    public void ReasoningContext_ProcessChunk_And_BuildFinalMessage()
    {
        var ctx = new ReasoningContext();
        ctx.ProcessChunk(new TextBlock { Text = "Hello" });
        ctx.ProcessChunk(new TextBlock { Text = " world" });
        Assert.Equal("Hello world", ctx.AccumulatedText);
        var msg = ctx.BuildFinalMessage();
        Assert.NotNull(msg);
        Assert.Equal("assistant", msg.Role);
        ctx.Reset();
        Assert.Equal("", ctx.AccumulatedText);
    }

    [Fact]
    public void ReasoningContext_ProcessChunk_WithThinking_AccumulatesThinking()
    {
        var ctx = new ReasoningContext();
        ctx.ProcessChunk(new ThinkingBlock { Thinking = "step1" });
        ctx.ProcessChunk(new ThinkingBlock { Thinking = "step2" });
        Assert.Equal("step1step2", ctx.AccumulatedThinking);
    }
}
