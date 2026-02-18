// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Tracing;

/// <summary>
/// Span exporter interface
/// Span 导出器接口
/// 
/// 参考: OpenTelemetry SpanExporter 概念
/// </summary>
public interface ISpanExporter
{
    /// <summary>
    /// Export a span
    /// 导出 Span
    /// </summary>
    void Export(TraceSpan span);

    /// <summary>
    /// Export multiple spans
    /// 导出多个 Span
    /// </summary>
    void ExportBatch(IEnumerable<TraceSpan> spans);

    /// <summary>
    /// Flush any buffered spans
    /// 刷新所有缓冲的 Span
    /// </summary>
    Task FlushAsync(CancellationToken ct = default);

    /// <summary>
    /// Shutdown the exporter
    /// 关闭导出器
    /// </summary>
    Task ShutdownAsync(CancellationToken ct = default);
}

/// <summary>
/// Console span exporter for debugging
/// 用于调试的控制台 Span 导出器
/// </summary>
public class ConsoleSpanExporter : ISpanExporter
{
    private readonly bool _includeAttributes;
    private readonly bool _includeEvents;

    /// <summary>
    /// Creates a new console exporter
    /// 创建新控制台导出器
    /// </summary>
    public ConsoleSpanExporter(bool includeAttributes = true, bool includeEvents = true)
    {
        _includeAttributes = includeAttributes;
        _includeEvents = includeEvents;
    }

    /// <inheritdoc />
    public void Export(TraceSpan span)
    {
        var duration = span.Duration?.TotalMilliseconds ?? 0;
        var status = span.Status == SpanStatusCode.Ok ? "✓" :
                     span.Status == SpanStatusCode.Error ? "✗" : "○";

        Console.WriteLine($"[{status}] {span.Name} ({duration:F2}ms) - {span.SpanId}");
        Console.WriteLine($"    Trace: {span.TraceId}, Parent: {span.ParentSpanId ?? "root"}");
        Console.WriteLine($"    Kind: {span.Kind}, Status: {span.Status}");

        if (_includeAttributes && span.Attributes.Count > 0)
        {
            Console.WriteLine("    Attributes:");
            foreach (var (key, value) in span.Attributes)
            {
                Console.WriteLine($"      {key}: {value}");
            }
        }

        if (_includeEvents && span.Events.Count > 0)
        {
            Console.WriteLine("    Events:");
            foreach (var evt in span.Events)
            {
                var evtDuration = (evt.Timestamp - span.StartTime).TotalMilliseconds;
                Console.WriteLine($"      [{evtDuration:F2}ms] {evt.Name}");
            }
        }

        Console.WriteLine();
    }

    /// <inheritdoc />
    public void ExportBatch(IEnumerable<TraceSpan> spans)
    {
        foreach (var span in spans)
        {
            Export(span);
        }
    }

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// In-memory span exporter for testing
/// 用于测试的内存 Span 导出器
/// </summary>
public class InMemorySpanExporter : ISpanExporter, IDisposable
{
    private readonly List<TraceSpan> _spans = new();
    private readonly object _lock = new();
    private bool _disposed;

    /// <summary>
    /// Exported spans
    /// 已导出的 Span
    /// </summary>
    public IReadOnlyList<TraceSpan> Spans
    {
        get
        {
            lock (_lock)
            {
                return _spans.ToList().AsReadOnly();
            }
        }
    }

    /// <summary>
    /// Number of exported spans
    /// 已导出的 Span 数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _spans.Count;
            }
        }
    }

    /// <inheritdoc />
    public void Export(TraceSpan span)
    {
        if (_disposed) return;

        lock (_lock)
        {
            _spans.Add(span);
        }
    }

    /// <inheritdoc />
    public void ExportBatch(IEnumerable<TraceSpan> spans)
    {
        if (_disposed) return;

        lock (_lock)
        {
            _spans.AddRange(spans);
        }
    }

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clear all spans
    /// 清除所有 Span
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _spans.Clear();
        }
    }

    /// <summary>
    /// Get spans by name
    /// 按名称获取 Span
    /// </summary>
    public IReadOnlyList<TraceSpan> GetSpansByName(string name)
    {
        lock (_lock)
        {
            return _spans.Where(s => s.Name == name).ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Get spans by trace ID
    /// 按 Trace ID 获取 Span
    /// </summary>
    public IReadOnlyList<TraceSpan> GetSpansByTraceId(string traceId)
    {
        lock (_lock)
        {
            return _spans.Where(s => s.TraceId == traceId).ToList().AsReadOnly();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            _spans.Clear();
        }
    }
}

/// <summary>
/// Composite span exporter that exports to multiple exporters
/// 组合 Span 导出器，导出到多个导出器
/// </summary>
public class CompositeSpanExporter : ISpanExporter
{
    private readonly List<ISpanExporter> _exporters = new();

    /// <summary>
    /// Creates a new composite exporter
    /// 创建新组合导出器
    /// </summary>
    public CompositeSpanExporter(params ISpanExporter[] exporters)
    {
        _exporters.AddRange(exporters);
    }

    /// <summary>
    /// Add an exporter
    /// 添加导出器
    /// </summary>
    public void AddExporter(ISpanExporter exporter)
    {
        _exporters.Add(exporter);
    }

    /// <inheritdoc />
    public void Export(TraceSpan span)
    {
        foreach (var exporter in _exporters)
        {
            try
            {
                exporter.Export(span);
            }
            catch (global::System.Exception ex)
            {
                Console.Error.WriteLine($"Exporter error: {ex.Message}");
            }
        }
    }

    /// <inheritdoc />
    public void ExportBatch(IEnumerable<TraceSpan> spans)
    {
        foreach (var exporter in _exporters)
        {
            try
            {
                exporter.ExportBatch(spans);
            }
            catch (global::System.Exception ex)
            {
                Console.Error.WriteLine($"Exporter error: {ex.Message}");
            }
        }
    }

    /// <inheritdoc />
    public async Task FlushAsync(CancellationToken ct = default)
    {
        foreach (var exporter in _exporters)
        {
            await exporter.FlushAsync(ct);
        }
    }

    /// <inheritdoc />
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        foreach (var exporter in _exporters)
        {
            await exporter.ShutdownAsync(ct);
        }
    }
}
