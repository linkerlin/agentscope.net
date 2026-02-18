// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tracing;
using Xunit;

namespace AgentScope.Core.Tests.Tracing;

public class SpanExporterTests
{
    [Fact]
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
    public void TraceContext_FromTraceParent_InvalidFormat_ReturnsNull()
    {
        // Arrange
        var traceParent = "invalid";

        // Act
        var context = TraceContext.FromTraceParent(traceParent);

        // Assert
        Assert.Null(context);
    }

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
