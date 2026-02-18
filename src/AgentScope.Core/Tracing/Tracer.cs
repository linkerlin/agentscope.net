// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Concurrent;

namespace AgentScope.Core.Tracing;

/// <summary>
/// Default implementation of ITracer
/// ITracer 的默认实现
/// </summary>
public class Tracer : ITracer
{
    private readonly ConcurrentDictionary<string, ISpan> _activeSpans = new();
    private readonly ISpanExporter? _exporter;
    private readonly Sampler _sampler;

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public ISpan? CurrentSpan
    {
        get
        {
            var currentId = AsyncLocalContext.CurrentSpanId;
            if (string.IsNullOrEmpty(currentId)) return null;
            _activeSpans.TryGetValue(currentId, out var span);
            return span;
        }
    }

    /// <summary>
    /// Creates a new tracer
    /// 创建新追踪器
    /// </summary>
    public Tracer(string name, ISpanExporter? exporter = null, Sampler? sampler = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _exporter = exporter;
        _sampler = sampler ?? new AlwaysOnSampler();
    }

    /// <inheritdoc />
    public ISpan StartSpan(
        string name,
        SpanKind kind = SpanKind.Internal,
        ISpan? parentSpan = null,
        Dictionary<string, object>? attributes = null)
    {
        var traceId = parentSpan?.TraceId ?? GenerateTraceId();
        var parentSpanId = parentSpan?.SpanId;

        return StartSpan(name, traceId, parentSpanId, kind, attributes);
    }

    /// <inheritdoc />
    public ISpan StartSpan(
        string name,
        string traceId,
        string? parentSpanId,
        SpanKind kind = SpanKind.Internal,
        Dictionary<string, object>? attributes = null)
    {
        // Check sampling
        var context = new TraceContext
        {
            TraceId = traceId,
            SpanId = parentSpanId ?? string.Empty,
            IsSampled = true
        };

        if (!_sampler.ShouldSample(context))
        {
            context.IsSampled = false;
        }

        var spanId = GenerateSpanId();

        // Only set callback if sampling
        Action<TraceSpan>? onEnd = context.IsSampled ? OnSpanEnd : null;

        var span = new TraceSpan(
            name,
            traceId,
            spanId,
            parentSpanId,
            kind,
            attributes,
            onEnd);

        if (context.IsSampled)
        {
            _activeSpans[spanId] = span;
            AsyncLocalContext.CurrentSpanId = spanId;
        }

        return span;
    }

    /// <inheritdoc />
    public TraceContext CreateContext()
    {
        return new TraceContext
        {
            TraceId = GenerateTraceId(),
            SpanId = GenerateSpanId(),
            IsSampled = true
        };
    }

    /// <summary>
    /// Create a span builder
    /// 创建 Span 构建器
    /// </summary>
    public SpanBuilder SpanBuilder(string name)
    {
        return new SpanBuilder(name);
    }

    private void OnSpanEnd(TraceSpan span)
    {
        _activeSpans.TryRemove(span.SpanId, out _);
        _exporter?.Export(span);
    }

    private static string GenerateTraceId()
    {
        return Guid.NewGuid().ToString("N").ToLowerInvariant();
    }

    private static string GenerateSpanId()
    {
        return Guid.NewGuid().ToString("N")[..16].ToLowerInvariant();
    }
}

/// <summary>
/// Async local context for current span
/// 当前 Span 的异步本地上下文
/// </summary>
internal static class AsyncLocalContext
{
    private static readonly AsyncLocal<string> _currentSpanId = new();

    public static string? CurrentSpanId
    {
        get => _currentSpanId.Value;
        set => _currentSpanId.Value = value ?? string.Empty;
    }
}

/// <summary>
/// Sampler interface for trace sampling
/// 追踪采样器接口
/// </summary>
public abstract class Sampler
{
    /// <summary>
    /// Determine if a trace should be sampled
    /// 决定是否应该采样追踪
    /// </summary>
    public abstract bool ShouldSample(TraceContext context);
}

/// <summary>
/// Always-on sampler (sample everything)
/// 总是采样（采样所有）
/// </summary>
public class AlwaysOnSampler : Sampler
{
    public override bool ShouldSample(TraceContext context) => true;
}

/// <summary>
/// Always-off sampler (sample nothing)
/// 总是不采样（不采样任何）
/// </summary>
public class AlwaysOffSampler : Sampler
{
    public override bool ShouldSample(TraceContext context) => false;
}

/// <summary>
/// Probability-based sampler
/// 基于概率的采样器
/// </summary>
public class ProbabilitySampler : Sampler
{
    private readonly double _probability;
    private readonly Random _random = new();

    public ProbabilitySampler(double probability)
    {
        if (probability < 0 || probability > 1)
            throw new ArgumentOutOfRangeException(nameof(probability), "Probability must be between 0 and 1");
        _probability = probability;
    }

    public override bool ShouldSample(TraceContext context)
    {
        return _random.NextDouble() < _probability;
    }
}
