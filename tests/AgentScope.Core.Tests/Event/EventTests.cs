// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Message;
using Xunit;
using EventItem = AgentScope.Core.Events.Event;
using EventType = AgentScope.Core.Events.EventType;

namespace AgentScope.Core.Tests.Event;

/// <summary>
/// Tests for Event and EventType types
/// Event 和 EventType 类型测试
/// </summary>
public class EventTests
{
    /// <summary>
    /// Tests that the Event constructor properly sets Type, Message, IsLast, and Metadata properties.
    /// 测试 Event 构造函数正确设置 Type、Message、IsLast 和 Metadata 属性。
    /// </summary>
    [Fact]
    public void Event_Constructor_SetsProperties()
    {
        var msg = Msg.Builder().TextContent("test").Build();
        var ev = new EventItem(EventType.ReasoningStart, msg, isLast: false);
        Assert.Equal(EventType.ReasoningStart, ev.Type);
        Assert.Same(msg, ev.Message);
        Assert.False(ev.IsLast);
        Assert.NotNull(ev.Metadata);
        Assert.Empty(ev.Metadata);
    }

    /// <summary>
    /// Tests that Event stores and retrieves metadata correctly.
    /// 测试 Event 正确存储和检索元数据。
    /// </summary>
    [Fact]
    public void Event_WithMetadata_StoresMetadata()
    {
        var meta = new Dictionary<string, object> { ["key"] = "value" };
        var ev = new EventItem(EventType.ReasoningChunk, null, false, meta);
        Assert.Equal("value", ev.Metadata["key"]);
    }

    /// <summary>
    /// Tests that IsLast=true correctly indicates this is the terminal event in a sequence.
    /// 测试 IsLast=true 正确表示这是序列中的终止事件。
    /// </summary>
    [Fact]
    public void Event_IsLast_True_IndicatesTermination()
    {
        var ev = new EventItem(EventType.ReasoningFinish, null, isLast: true);
        Assert.True(ev.IsLast);
    }

    /// <summary>
    /// Tests that IsReasoning returns true for reasoning-related EventTypes and false for others.
    /// 测试 IsReasoning 对推理相关的事件类型返回 true，其他返回 false。
    /// </summary>
    [Fact]
    public void Event_IsReasoning_ForReasoningTypes()
    {
        Assert.True(new EventItem(EventType.ReasoningStart, null).IsReasoning);
        Assert.True(new EventItem(EventType.ReasoningChunk, null).IsReasoning);
        Assert.True(new EventItem(EventType.ReasoningFinish, null).IsReasoning);
        Assert.False(new EventItem(EventType.ToolCallStart, null).IsReasoning);
    }

    /// <summary>
    /// Tests that IsToolCall returns true for tool-call-related EventTypes and false for others.
    /// 测试 IsToolCall 对工具调用相关的事件类型返回 true，其他返回 false。
    /// </summary>
    [Fact]
    public void Event_IsToolCall_ForToolCallTypes()
    {
        Assert.True(new EventItem(EventType.ToolCallStart, null).IsToolCall);
        Assert.True(new EventItem(EventType.ToolCallChunk, null).IsToolCall);
        Assert.True(new EventItem(EventType.ToolCallFinish, null).IsToolCall);
        Assert.False(new EventItem(EventType.ActingStart, null).IsToolCall);
    }

    /// <summary>
    /// Tests that IsActing returns true for acting-related EventTypes.
    /// 测试 IsActing 对行动相关的事件类型返回 true。
    /// </summary>
    [Fact]
    public void Event_IsActing_ForActingTypes()
    {
        Assert.True(new EventItem(EventType.ActingStart, null).IsActing);
        Assert.True(new EventItem(EventType.ActingChunk, null).IsActing);
        Assert.True(new EventItem(EventType.ActingFinish, null).IsActing);
    }

    [Fact]
    public void Event_IsSummary_ForSummaryTypes()
    {
        Assert.True(new EventItem(EventType.SummaryStart, null).IsSummary);
        Assert.True(new EventItem(EventType.SummaryChunk, null).IsSummary);
        Assert.True(new EventItem(EventType.SummaryFinish, null).IsSummary);
    }

    [Fact]
    public void Event_IsError_ForErrorType()
    {
        Assert.True(new EventItem(EventType.Error, null).IsError);
        Assert.False(new EventItem(EventType.ReasoningFinish, null).IsError);
    }

    [Fact]
    public void ErrorEvent_Static_CreatesErrorWithMessage()
    {
        var msg = Msg.Builder().TextContent("err").Build();
        var ev = EventItem.ErrorEvent(msg, "something failed", isLast: true);
        Assert.Equal(EventType.Error, ev.Type);
        Assert.True(ev.IsLast);
        Assert.True(ev.IsError);
        Assert.Equal("something failed", ev.Metadata["error"]);
    }

    [Fact]
    public void EventSequence_IsLast_OnlyOnFinalEvent()
    {
        var events = new[]
        {
            new EventItem(EventType.ReasoningStart, null, false),
            new EventItem(EventType.ReasoningChunk, null, false),
            new EventItem(EventType.ReasoningFinish, null, true)
        };
        Assert.False(events[0].IsLast);
        Assert.False(events[1].IsLast);
        Assert.True(events[2].IsLast);
    }

    [Fact]
    public void EventType_AllValues_Defined()
    {
        var names = Enum.GetNames(typeof(EventType));
        Assert.Contains("ReasoningStart", names);
        Assert.Contains("ReasoningChunk", names);
        Assert.Contains("ReasoningFinish", names);
        Assert.Contains("ToolCallStart", names);
        Assert.Contains("ToolCallFinish", names);
        Assert.Contains("ActingStart", names);
        Assert.Contains("ActingFinish", names);
        Assert.Contains("SummaryStart", names);
        Assert.Contains("SummaryChunk", names);
        Assert.Contains("SummaryFinish", names);
        Assert.Contains("Error", names);
    }
}
