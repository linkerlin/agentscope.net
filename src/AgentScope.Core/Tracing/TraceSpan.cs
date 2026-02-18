// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using System.Collections.Concurrent;

namespace AgentScope.Core.Tracing;

/// <summary>
/// Default implementation of ISpan
/// ISpan 的默认实现
/// </summary>
public class TraceSpan : ISpan
{
    private readonly ConcurrentDictionary<string, object> _attributes = new();
    private readonly List<TraceEvent> _events = new();
    private readonly object _lock = new();
    private SpanStatusCode _status = SpanStatusCode.Unset;
    private string? _statusDescription;
    private DateTime? _endTime;
    private readonly Action<TraceSpan>? _onEnd;

    /// <inheritdoc />
    public string SpanId { get; }

    /// <inheritdoc />
    public string TraceId { get; }

    /// <inheritdoc />
    public string? ParentSpanId { get; }

    /// <inheritdoc />
    public string Name { get; }

    /// <inheritdoc />
    public SpanKind Kind { get; }

    /// <inheritdoc />
    public DateTime StartTime { get; }

    /// <inheritdoc />
    public DateTime? EndTime
    {
        get
        {
            lock (_lock)
            {
                return _endTime;
            }
        }
    }

    /// <inheritdoc />
    public TimeSpan? Duration
    {
        get
        {
            lock (_lock)
            {
                if (_endTime.HasValue)
                {
                    return _endTime.Value - StartTime;
                }
                return null;
            }
        }
    }

    /// <inheritdoc />
    public SpanStatusCode Status
    {
        get
        {
            lock (_lock)
            {
                return _status;
            }
        }
    }

    /// <inheritdoc />
    public string? StatusDescription
    {
        get
        {
            lock (_lock)
            {
                return _statusDescription;
            }
        }
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Attributes => _attributes;

    /// <inheritdoc />
    public IReadOnlyList<TraceEvent> Events
    {
        get
        {
            lock (_lock)
            {
                return _events.ToList().AsReadOnly();
            }
        }
    }

    /// <inheritdoc />
    public bool HasEnded
    {
        get
        {
            lock (_lock)
            {
                return _endTime.HasValue;
            }
        }
    }

    /// <summary>
    /// Creates a new trace span
    /// 创建新追踪 Span
    /// </summary>
    public TraceSpan(
        string name,
        string traceId,
        string spanId,
        string? parentSpanId,
        SpanKind kind,
        Dictionary<string, object>? attributes,
        Action<TraceSpan>? onEnd = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        TraceId = traceId ?? throw new ArgumentNullException(nameof(traceId));
        SpanId = spanId ?? throw new ArgumentNullException(nameof(spanId));
        ParentSpanId = parentSpanId;
        Kind = kind;
        StartTime = DateTime.UtcNow;
        _onEnd = onEnd;

        if (attributes != null)
        {
            foreach (var (key, value) in attributes)
            {
                _attributes[key] = value;
            }
        }
    }

    /// <inheritdoc />
    public ISpan SetAttribute(string key, object value)
    {
        if (HasEnded) return this;

        _attributes[key] = value ?? throw new ArgumentNullException(nameof(value));
        return this;
    }

    /// <inheritdoc />
    public ISpan AddEvent(string name, DateTime? timestamp = null, Dictionary<string, object>? attributes = null)
    {
        if (HasEnded) return this;

        var evt = new TraceEvent
        {
            Name = name,
            Timestamp = timestamp ?? DateTime.UtcNow,
            Attributes = attributes ?? new Dictionary<string, object>()
        };

        lock (_lock)
        {
            _events.Add(evt);
        }

        return this;
    }

    /// <inheritdoc />
    public ISpan RecordException(global::System.Exception exception, Dictionary<string, object>? attributes = null)
    {
        if (HasEnded) return this;

        var attrs = attributes ?? new Dictionary<string, object>();
        attrs["exception.type"] = exception.GetType().FullName ?? "Unknown";
        attrs["exception.message"] = exception.Message;
        attrs["exception.stacktrace"] = exception.StackTrace ?? string.Empty;

        AddEvent("exception", DateTime.UtcNow, attrs);
        SetStatus(SpanStatusCode.Error, exception.Message);

        return this;
    }

    /// <inheritdoc />
    public ISpan SetStatus(SpanStatusCode status, string? description = null)
    {
        if (HasEnded) return this;

        lock (_lock)
        {
            _status = status;
            _statusDescription = description;
        }

        return this;
    }

    /// <inheritdoc />
    public void End(DateTime? endTime = null)
    {
        lock (_lock)
        {
            if (_endTime.HasValue) return;
            _endTime = endTime ?? DateTime.UtcNow;
        }

        _onEnd?.Invoke(this);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!HasEnded)
        {
            End();
        }
    }
}

/// <summary>
/// Span builder for fluent API
/// Span 构建器（流畅 API）
/// </summary>
public class SpanBuilder
{
    private readonly string _name;
    private SpanKind _kind = SpanKind.Internal;
    private string? _traceId;
    private string? _parentSpanId;
    private Dictionary<string, object>? _attributes;

    public SpanBuilder(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Set span kind
    /// </summary>
    public SpanBuilder WithKind(SpanKind kind)
    {
        _kind = kind;
        return this;
    }

    /// <summary>
    /// Set parent span
    /// </summary>
    public SpanBuilder WithParent(ISpan parent)
    {
        _traceId = parent.TraceId;
        _parentSpanId = parent.SpanId;
        return this;
    }

    /// <summary>
    /// Set trace and parent IDs
    /// </summary>
    public SpanBuilder WithParent(string traceId, string? parentSpanId)
    {
        _traceId = traceId;
        _parentSpanId = parentSpanId;
        return this;
    }

    /// <summary>
    /// Add attribute
    /// </summary>
    public SpanBuilder WithAttribute(string key, object value)
    {
        _attributes ??= new Dictionary<string, object>();
        _attributes[key] = value;
        return this;
    }

    /// <summary>
    /// Add attributes
    /// </summary>
    public SpanBuilder WithAttributes(Dictionary<string, object> attributes)
    {
        _attributes ??= new Dictionary<string, object>();
        foreach (var (key, value) in attributes)
        {
            _attributes[key] = value;
        }
        return this;
    }

    /// <summary>
    /// Build the span
    /// </summary>
    public TraceSpan Build(Action<TraceSpan>? onEnd = null)
    {
        var traceId = _traceId ?? GenerateTraceId();
        var spanId = GenerateSpanId();

        return new TraceSpan(
            _name,
            traceId,
            spanId,
            _parentSpanId,
            _kind,
            _attributes,
            onEnd);
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
