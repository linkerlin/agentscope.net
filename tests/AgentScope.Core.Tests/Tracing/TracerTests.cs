// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tracing;
using Xunit;

namespace AgentScope.Core.Tests.Tracing;

/// <summary>
/// Tests for Tracer and TracerProvider
/// Tracer 和 TracerProvider 的测试
/// </summary>
public class TracerTests
{
    [Fact]
    /// <summary>
    /// Tracer constructor sets the tracer name
    /// 测试 Tracer 构造器设置跟踪器名称
    /// </summary>
    public void Constructor_WithName_SetsName()
    {
        // Arrange & Act
        var tracer = new Tracer("test-tracer");

        // Assert
        Assert.Equal("test-tracer", tracer.Name);
    }

    [Fact]
    /// <summary>
    /// Tracer.StartSpan creates a span with the given name
    /// 测试 Tracer.StartSpan 创建具有指定名称的 span
    /// </summary>
    public void StartSpan_WithName_CreatesSpan()
    {
        // Arrange
        var tracer = new Tracer("test-tracer");

        // Act
        using var span = tracer.StartSpan("test-operation");

        // Assert
        Assert.NotNull(span);
        Assert.Equal("test-operation", span.Name);
        Assert.Equal(SpanKind.Internal, span.Kind);
        Assert.NotNull(span.SpanId);
        Assert.NotNull(span.TraceId);
    }

    [Fact]
    /// <summary>
    /// Tracer.StartSpan sets the span kind
    /// 测试 Tracer.StartSpan 设置 span 的 kind 属性
    /// </summary>
    public void StartSpan_WithKind_SetsKind()
    {
        // Arrange
        var tracer = new Tracer("test-tracer");

        // Act
        using var span = tracer.StartSpan("test-operation", SpanKind.Server);

        // Assert
        Assert.Equal(SpanKind.Server, span.Kind);
    }

    [Fact]
    /// <summary>
    /// Tracer.StartSpan with a parent span creates a child span sharing the trace ID
    /// 测试 Tracer.StartSpan 使用父 span 创建共享 trace ID 的子 span
    /// </summary>
    public void StartSpan_WithParent_CreatesChildSpan()
    {
        // Arrange
        var tracer = new Tracer("test-tracer");
        using var parent = tracer.StartSpan("parent-operation");

        // Act
        using var child = tracer.StartSpan("child-operation", SpanKind.Internal, parent);

        // Assert
        Assert.Equal(parent.TraceId, child.TraceId);
        Assert.Equal(parent.SpanId, child.ParentSpanId);
    }

    [Fact]
    /// <summary>
    /// Tracer.StartSpan with explicit trace ID and parent span ID sets the IDs
    /// 测试 Tracer.StartSpan 使用显式 trace ID 和父 span ID 设置标识符
    /// </summary>
    public void StartSpan_WithExplicitIds_SetsIds()
    {
        // Arrange
        var tracer = new Tracer("test-tracer");
        var traceId = Guid.NewGuid().ToString("N");
        var parentSpanId = Guid.NewGuid().ToString("N")[..16];

        // Act
        using var span = tracer.StartSpan("test-operation", traceId, parentSpanId);

        // Assert
        Assert.Equal(traceId, span.TraceId);
        Assert.Equal(parentSpanId, span.ParentSpanId);
    }

    [Fact]
    /// <summary>
    /// Tracer.StartSpan with attributes sets them on the created span
    /// 测试 Tracer.StartSpan 在创建 span 时设置属性
    /// </summary>
    public void StartSpan_WithAttributes_SetsAttributes()
    {
        // Arrange
        var tracer = new Tracer("test-tracer");
        var attributes = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        using var span = tracer.StartSpan("test-operation", attributes: attributes);

        // Assert
        Assert.Equal("value1", span.Attributes["key1"]);
        Assert.Equal(42, span.Attributes["key2"]);
    }

    [Fact]
    /// <summary>
    /// Tracer.CurrentSpan returns null when there is no active span
    /// 测试 Tracer.CurrentSpan 在没有活动 span 时返回 null
    /// </summary>
    public void CurrentSpan_NoActiveSpan_ReturnsNull()
    {
        // Arrange
        var tracer = new Tracer("test-tracer");

        // Act
        var current = tracer.CurrentSpan;

        // Assert
        Assert.Null(current);
    }

    [Fact]
    /// <summary>
    /// Tracer.CreateContext generates a valid trace context
    /// 测试 Tracer.CreateContext 生成有效的跟踪上下文
    /// </summary>
    public void CreateContext_GeneratesValidContext()
    {
        // Arrange
        var tracer = new Tracer("test-tracer");

        // Act
        var context = tracer.CreateContext();

        // Assert
        Assert.NotNull(context);
        Assert.NotNull(context.TraceId);
        Assert.NotNull(context.SpanId);
        Assert.True(context.IsSampled);
    }

    [Fact]
    /// <summary>
    /// Tracer.SpanBuilder builds a span with configured options
    /// 测试 Tracer.SpanBuilder 构建具有配置选项的 span
    /// </summary>
    public void SpanBuilder_BuildsSpan()
    {
        // Arrange
        var tracer = new Tracer("test-tracer");

        // Act
        var span = tracer.SpanBuilder("test-operation")
            .WithKind(SpanKind.Server)
            .WithAttribute("key", "value")
            .Build();

        // Assert
        Assert.Equal("test-operation", span.Name);
        Assert.Equal(SpanKind.Server, span.Kind);
        Assert.Equal("value", span.Attributes["key"]);
    }

    [Fact]
    public void TracerProvider_Initialize_SetsGlobalTracer()
    {
        // Arrange
        TracerProvider.GlobalTracer = null;

        // Act
        TracerProvider.Initialize("global-tracer");

        // Assert
        Assert.NotNull(TracerProvider.GlobalTracer);
        Assert.Equal("global-tracer", TracerProvider.GlobalTracer.Name);

        // Cleanup
        TracerProvider.GlobalTracer = null;
    }

    [Fact]
    public void TracerProvider_GetTracer_WhenNotInitialized_ReturnsNoOpTracer()
    {
        // Arrange
        TracerProvider.GlobalTracer = null;

        // Act
        var tracer = TracerProvider.GetTracer();

        // Assert
        Assert.NotNull(tracer);

        // Verify it's a no-op tracer (sampling is off)
        using var span = tracer.StartSpan("test");
        Assert.NotNull(span);
    }

    [Fact]
    public void TracerProvider_IsInitialized_WhenNotInitialized_ReturnsFalse()
    {
        // Arrange
        TracerProvider.GlobalTracer = null;

        // Act & Assert
        Assert.False(TracerProvider.IsInitialized);
    }

    [Fact]
    public void TracerProvider_IsInitialized_WhenInitialized_ReturnsTrue()
    {
        // Arrange
        TracerProvider.Initialize("test");

        // Act & Assert
        Assert.True(TracerProvider.IsInitialized);

        // Cleanup
        TracerProvider.GlobalTracer = null;
    }
}
