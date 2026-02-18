// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tracing;
using Xunit;

namespace AgentScope.Core.Tests.Tracing;

public class TraceSpanTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        // Arrange & Act
        var span = new TraceSpan(
            "test-operation",
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("N")[..16],
            null,
            SpanKind.Server,
            null);

        // Assert
        Assert.Equal("test-operation", span.Name);
        Assert.Equal(SpanKind.Server, span.Kind);
        Assert.NotNull(span.SpanId);
        Assert.NotNull(span.TraceId);
        Assert.Null(span.ParentSpanId);
        Assert.Equal(SpanStatusCode.Unset, span.Status);
        Assert.False(span.HasEnded);
        Assert.Null(span.EndTime);
        Assert.Null(span.Duration);
    }

    [Fact]
    public void SetAttribute_AddsAttribute()
    {
        // Arrange
        var span = CreateTestSpan();

        // Act
        span.SetAttribute("key", "value");

        // Assert
        Assert.Equal("value", span.Attributes["key"]);
    }

    [Fact]
    public void SetAttribute_WhenEnded_DoesNotAdd()
    {
        // Arrange
        var span = CreateTestSpan();
        span.End();

        // Act
        span.SetAttribute("key", "value");

        // Assert
        Assert.Empty(span.Attributes);
    }

    [Fact]
    public void AddEvent_AddsEvent()
    {
        // Arrange
        var span = CreateTestSpan();

        // Act
        span.AddEvent("test-event");

        // Assert
        Assert.Single(span.Events);
        Assert.Equal("test-event", span.Events[0].Name);
    }

    [Fact]
    public void AddEvent_WithAttributes_AddsEventWithAttributes()
    {
        // Arrange
        var span = CreateTestSpan();
        var attrs = new Dictionary<string, object> { ["key"] = "value" };

        // Act
        span.AddEvent("test-event", attributes: attrs);

        // Assert
        Assert.Single(span.Events);
        Assert.Equal("value", span.Events[0].Attributes["key"]);
    }

    [Fact]
    public void RecordException_AddsExceptionEvent()
    {
        // Arrange
        var span = CreateTestSpan();
        var exception = new InvalidOperationException("Test error");

        // Act
        span.RecordException(exception);

        // Assert
        Assert.Single(span.Events);
        Assert.Equal("exception", span.Events[0].Name);
        Assert.Equal(SpanStatusCode.Error, span.Status);
        Assert.Contains("Test error", span.StatusDescription ?? "");
    }

    [Fact]
    public void SetStatus_SetsStatus()
    {
        // Arrange
        var span = CreateTestSpan();

        // Act
        span.SetStatus(SpanStatusCode.Ok, "Success");

        // Assert
        Assert.Equal(SpanStatusCode.Ok, span.Status);
        Assert.Equal("Success", span.StatusDescription);
    }

    [Fact]
    public void End_SetsEndTime()
    {
        // Arrange
        var span = CreateTestSpan();
        var beforeEnd = DateTime.UtcNow;

        // Act
        span.End();
        var afterEnd = DateTime.UtcNow;

        // Assert
        Assert.True(span.HasEnded);
        Assert.NotNull(span.EndTime);
        Assert.True(span.EndTime >= beforeEnd);
        Assert.True(span.EndTime <= afterEnd);
        Assert.NotNull(span.Duration);
    }

    [Fact]
    public void End_WhenCalledTwice_OnlyEndsOnce()
    {
        // Arrange
        var span = CreateTestSpan();

        // Act
        span.End();
        var firstEndTime = span.EndTime;
        System.Threading.Thread.Sleep(10);
        span.End();
        var secondEndTime = span.EndTime;

        // Assert
        Assert.Equal(firstEndTime, secondEndTime);
    }

    [Fact]
    public void Dispose_EndsSpan()
    {
        // Arrange
        var span = CreateTestSpan();

        // Act
        span.Dispose();

        // Assert
        Assert.True(span.HasEnded);
    }

    [Fact]
    public void SpanBuilder_BuildsWithAllOptions()
    {
        // Arrange
        var builder = new SpanBuilder("test-operation")
            .WithKind(SpanKind.Client)
            .WithAttribute("key1", "value1")
            .WithAttributes(new Dictionary<string, object> { ["key2"] = "value2" });

        // Act
        var span = builder.Build();

        // Assert
        Assert.Equal("test-operation", span.Name);
        Assert.Equal(SpanKind.Client, span.Kind);
        Assert.Equal("value1", span.Attributes["key1"]);
        Assert.Equal("value2", span.Attributes["key2"]);
    }

    [Fact]
    public void SpanBuilder_WithParent_SetsParent()
    {
        // Arrange
        var parent = CreateTestSpan();
        var builder = new SpanBuilder("child-operation")
            .WithParent(parent.TraceId, parent.SpanId);

        // Act
        var child = builder.Build();

        // Assert
        Assert.Equal(parent.TraceId, child.TraceId);
        Assert.Equal(parent.SpanId, child.ParentSpanId);
    }

    [Fact]
    public void SpanBuilder_WithoutTraceId_GeneratesNewId()
    {
        // Arrange
        var builder = new SpanBuilder("test-operation");

        // Act
        var span = builder.Build();

        // Assert
        Assert.NotNull(span.TraceId);
        Assert.NotEmpty(span.TraceId);
    }

    private static TraceSpan CreateTestSpan()
    {
        return new TraceSpan(
            "test-operation",
            Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("N")[..16],
            null,
            SpanKind.Internal,
            null);
    }
}
