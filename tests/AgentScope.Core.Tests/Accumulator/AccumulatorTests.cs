// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Accumulator;
using AgentScope.Core.Message;
using Xunit;

namespace AgentScope.Core.Tests.Accumulator;

/// <summary>
/// Tests for Accumulator components (TextAccumulator, ThinkingAccumulator, ToolCallsAccumulator, ReasoningContext)
/// 累加器组件测试（TextAccumulator、ThinkingAccumulator、ToolCallsAccumulator、ReasoningContext）
/// </summary>
public class AccumulatorTests
{
    /// <summary>
    /// Tests that TextAccumulator accumulates text blocks and returns merged result, and Reset clears state.
    /// 测试 TextAccumulator 累加文本块并返回合并结果，Reset 清除状态。
    /// </summary>
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

    /// <summary>
    /// Tests that ThinkingAccumulator accumulates thinking blocks and returns the concatenated thinking text.
    /// 测试 ThinkingAccumulator 累加 thinking 块并返回拼接后的思考文本。
    /// </summary>
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

    /// <summary>
    /// Tests that ToolCallsAccumulator collects tool use and tool result blocks.
    /// 测试 ToolCallsAccumulator 收集工具使用和工具结果块。
    /// </summary>
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

    /// <summary>
    /// Tests that ReasoningContext processes text chunks and builds a final assistant message.
    /// 测试 ReasoningContext 处理文本块并构建最终的 assistant 消息。
    /// </summary>
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

    /// <summary>
    /// Tests that ReasoningContext accumulates thinking blocks separately from text.
    /// 测试 ReasoningContext 将 thinking 块与文本块分开累加。
    /// </summary>
    [Fact]
    public void ReasoningContext_ProcessChunk_WithThinking_AccumulatesThinking()
    {
        var ctx = new ReasoningContext();
        ctx.ProcessChunk(new ThinkingBlock { Thinking = "step1" });
        ctx.ProcessChunk(new ThinkingBlock { Thinking = "step2" });
        Assert.Equal("step1step2", ctx.AccumulatedThinking);
    }
}
