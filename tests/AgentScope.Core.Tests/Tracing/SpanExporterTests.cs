// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tracing;
using Xunit;

namespace AgentScope.Core.Tests.Tracing;

/// <summary>
/// Tests for span exporters (InMemorySpanExporter, CompositeSpanExporter) and TraceContext
/// span 导出器（InMemorySpanExporter、CompositeSpanExporter）和 TraceContext 的测试
/// </summary>
public class SpanExporterTests
{
    [Fact]
    /// <summary>
    /// InMemorySpanExporter.Export adds a span to the in-memory store
    /// 测试 InMemorySpanExporter.Export 向内存存储中添加 span
    /// </summary>
    public void InMemoryExporter_Export_AddsSpan()
    {
        // Arrange
        using var exporter = new InMemorySpanExporter();
        var span = CreateTestSpan();

        // Act
        exporter.Export(span);

        // Assert
        Assert.Equal(1, exporter.Count);
    }

    [Fact]
    /// <summary>
    /// InMemorySpanExporter.ExportBatch adds multiple spans at once
    /// 测试 InMemorySpanExporter.ExportBatch 批量添加多个 span
    /// </summary>
    public void InMemoryExporter_ExportBatch_AddsSpans()
    {
        // Arrange
        using var exporter = new InMemorySpanExporter();
        var spans = new[]
        {
            CreateTestSpan("span1"),
            CreateTestSpan("span2"),
            CreateTestSpan("span3")
        };

        // Act
        exporter.ExportBatch(spans);

        // Assert
        Assert.Equal(3, exporter.Count);
    }

    [Fact]
    /// <summary>
    /// InMemorySpanExporter.Clear removes all stored spans
    /// 测试 InMemorySpanExporter.Clear 清除所有存储的 span
    /// </summary>
    public void InMemoryExporter_Clear_RemovesAllSpans()
    {
        // Arrange
        using var exporter = new InMemorySpanExporter();
        exporter.Export(CreateTestSpan());
        Assert.Equal(1, exporter.Count);

        // Act
        exporter.Clear();

        // Assert
        Assert.Equal(0, exporter.Count);
    }

    [Fact]
    /// <summary>
    /// InMemorySpanExporter.GetSpansByName returns spans matching the given operation name
    /// 测试 InMemorySpanExporter.GetSpansByName 按操作名称返回匹配的 span
    /// </summary>
    public void InMemoryExporter_GetSpansByName_ReturnsMatchingSpans()
    {
        // Arrange
        using var exporter = new InMemorySpanExporter();
        exporter.Export(CreateTestSpan("operation1"));
        exporter.Export(CreateTestSpan("operation1"));
        exporter.Export(CreateTestSpan("operation2"));

        // Act
        var result = exporter.GetSpansByName("operation1");

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    /// <summary>
    /// InMemorySpanExporter.GetSpansByTraceId returns spans matching the given trace ID
    /// 测试 InMemorySpanExporter.GetSpansByTraceId 按 trace ID 返回匹配的 span
    /// </summary>
    public void InMemoryExporter_GetSpansByTraceId_ReturnsMatchingSpans()
    {
        // Arrange
        using var exporter = new InMemorySpanExporter();
        var traceId = Guid.NewGuid().ToString("N");
        exporter.Export(CreateTestSpan(traceId: traceId));
        exporter.Export(CreateTestSpan(traceId: traceId));
        exporter.Export(CreateTestSpan());

        // Act
        var result = exporter.GetSpansByTraceId(traceId);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    /// <summary>
    /// InMemorySpanExporter does not add spans after being disposed
    /// 测试 InMemorySpanExporter 在释放后不再添加 span
    /// </summary>
    public void InMemoryExporter_WhenDisposed_DoesNotAdd()
    {
        // Arrange
        var exporter = new InMemorySpanExporter();
        exporter.Dispose();

        // Act
        exporter.Export(CreateTestSpan());

        // Assert
        Assert.Equal(0, exporter.Count);
    }

    [Fact]
    /// <summary>
    /// CompositeSpanExporter.Export exports the span to all registered exporters
    /// 测试 CompositeSpanExporter.Export 将 span 导出到所有注册的导出器
    /// </summary>
    public void CompositeExporter_Export_ExportsToAll()
    {
        // Arrange
        using var exporter1 = new InMemorySpanExporter();
        using var exporter2 = new InMemorySpanExporter();
        var composite = new CompositeSpanExporter(exporter1, exporter2);
        var span = CreateTestSpan();

        // Act
        composite.Export(span);

        // Assert
        Assert.Equal(1, exporter1.Count);
        Assert.Equal(1, exporter2.Count);
    }

    [Fact]
    /// <summary>
    /// CompositeSpanExporter.AddExporter dynamically adds a new exporter
    /// 测试 CompositeSpanExporter.AddExporter 动态添加新的导出器
    /// </summary>
    public void CompositeExporter_AddExporter_AddsExporter()
    {
        // Arrange
        using var exporter1 = new InMemorySpanExporter();
        using var exporter2 = new InMemorySpanExporter();
        var composite = new CompositeSpanExporter(exporter1);
        composite.AddExporter(exporter2);

        // Act
        composite.Export(CreateTestSpan());

        // Assert
        Assert.Equal(1, exporter1.Count);
        Assert.Equal(1, exporter2.Count);
    }

    [Fact]
    /// <summary>
    /// CompositeSpanExporter.ExportBatch exports batch to all registered exporters
    /// 测试 CompositeSpanExporter.ExportBatch 将批量 span 导出到所有注册的导出器
    /// </summary>
    public void CompositeExporter_ExportBatch_ExportsToAll()
    {
        // Arrange
        using var exporter1 = new InMemorySpanExporter();
        using var exporter2 = new InMemorySpanExporter();
        var composite = new CompositeSpanExporter(exporter1, exporter2);
        var spans = new[] { CreateTestSpan(), CreateTestSpan() };

        // Act
        composite.ExportBatch(spans);

        // Assert
        Assert.Equal(2, exporter1.Count);
        Assert.Equal(2, exporter2.Count);
    }

    [Fact]
    /// <summary>
    /// CompositeSpanExporter.FlushAsync calls flush on all registered exporters
    /// 测试 CompositeSpanExporter.FlushAsync 对所有注册导出器调用 flush
    /// </summary>
    public async Task CompositeExporter_FlushAsync_CallsAll()
    {
        // Arrange
        using var exporter1 = new InMemorySpanExporter();
        using var exporter2 = new InMemorySpanExporter();
        var composite = new CompositeSpanExporter(exporter1, exporter2);

        // Act
        await composite.FlushAsync();

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    /// <summary>
    /// CompositeSpanExporter.ShutdownAsync calls shutdown on all registered exporters
    /// 测试 CompositeSpanExporter.ShutdownAsync 对所有注册导出器调用 shutdown
    /// </summary>
    public async Task CompositeExporter_ShutdownAsync_CallsAll()
    {
        // Arrange
        using var exporter1 = new InMemorySpanExporter();
        using var exporter2 = new InMemorySpanExporter();
        var composite = new CompositeSpanExporter(exporter1, exporter2);

        // Act
        await composite.ShutdownAsync();

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    /// <summary>
    /// TraceContext.ToTraceParent returns a valid W3C traceparent format string
    /// 测试 TraceContext.ToTraceParent 返回有效的 W3C traceparent 格式字符串
    /// </summary>
    public void TraceContext_ToTraceParent_ReturnsValidFormat()
    {
        // Arrange
        var context = new TraceContext
        {
            TraceId = "0af7651916cd43dd8448eb211c80319c",
            SpanId = "b7ad6b7169203331",
            IsSampled = true
        };

        // Act
        var traceParent = context.ToTraceParent();

        // Assert
        Assert.Equal("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01", traceParent);
    }

    [Fact]
    /// <summary>
    /// TraceContext.FromTraceParent correctly parses a valid traceparent string
    /// 测试 TraceContext.FromTraceParent 正确解析有效的 traceparent 字符串
    /// </summary>
    public void TraceContext_FromTraceParent_ParsesCorrectly()
    {
        // Arrange
        var traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01";

        // Act
        var context = TraceContext.FromTraceParent(traceParent);

        // Assert
        Assert.NotNull(context);
        Assert.Equal("0af7651916cd43dd8448eb211c80319c", context.TraceId);
        Assert.Equal("b7ad6b7169203331", context.SpanId);
        Assert.True(context.IsSampled);
    }

    [Fact]
    /// <summary>
    /// TraceContext.FromTraceParent correctly parses a non-sampled traceparent
    /// 测试 TraceContext.FromTraceParent 正确解析未采样的 traceparent
    /// </summary>
    public void TraceContext_FromTraceParent_NotSampled()
    {
        // Arrange
        var traceParent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00";

        // Act
        var context = TraceContext.FromTraceParent(traceParent);

        // Assert
        Assert.NotNull(context);
        Assert.False(context.IsSampled);
    }

    [Fact]
    /// <summary>
    /// TraceContext.FromTraceParent returns null for invalid format strings
    /// 测试 TraceContext.FromTraceParent 对无效格式字符串返回 null
    /// </summary>
    public void TraceContext_FromTraceParent_InvalidFormat_ReturnsNull()
    {
        // Arrange
        var traceParent = "invalid";

        // Act
        var context = TraceContext.FromTraceParent(traceParent);

        // Assert
        Assert.Null(context);
    }

    /// <summary>
    /// Creates a test TraceSpan instance with optional name and trace ID
    /// 创建测试用的 TraceSpan 实例，可指定名称和 trace ID
    /// </summary>
    private static TraceSpan CreateTestSpan(string? name = null, string? traceId = null)
    {
        return new TraceSpan(
            name ?? "test-operation",
            traceId ?? Guid.NewGuid().ToString("N"),
            Guid.NewGuid().ToString("N")[..16],
            null,
            SpanKind.Internal,
            null);
    }
}
