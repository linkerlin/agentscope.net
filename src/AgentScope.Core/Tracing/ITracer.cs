// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

namespace AgentScope.Core.Tracing;

/// <summary>
/// Span status code
/// Span 状态码
/// </summary>
public enum SpanStatusCode
{
    /// <summary>
    /// Span is unset
    /// Span 未设置
    /// </summary>
    Unset,

    /// <summary>
    /// Span completed successfully
    /// Span 成功完成
    /// </summary>
    Ok,

    /// <summary>
    /// Span encountered an error
    /// Span 遇到错误
    /// </summary>
    Error
}

/// <summary>
/// Span kind
/// Span 类型
/// </summary>
public enum SpanKind
{
    /// <summary>
    /// Internal span
    /// 内部 Span
    /// </summary>
    Internal,

    /// <summary>
    /// Server span (incoming request)
    /// 服务端 Span（接收请求）
    /// </summary>
    Server,

    /// <summary>
    /// Client span (outgoing request)
    /// 客户端 Span（发送请求）
    /// </summary>
    Client,

    /// <summary>
    /// Producer span
    /// 生产者 Span
    /// </summary>
    Producer,

    /// <summary>
    /// Consumer span
    /// 消费者 Span
    /// </summary>
    Consumer
}

/// <summary>
/// Represents a tracing span
/// 追踪 Span
/// 
/// 参考: OpenTelemetry Span 概念
/// </summary>
public interface ISpan : IDisposable
{
    /// <summary>
    /// Span ID
    /// </summary>
    string SpanId { get; }

    /// <summary>
    /// Trace ID
    /// </summary>
    string TraceId { get; }

    /// <summary>
    /// Parent span ID (null if root)
    /// 父 Span ID（根 Span 为 null）
    /// </summary>
    string? ParentSpanId { get; }

    /// <summary>
    /// Span name
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Span kind
    /// </summary>
    SpanKind Kind { get; }

    /// <summary>
    /// Start timestamp
    /// 开始时间戳
    /// </summary>
    DateTime StartTime { get; }

    /// <summary>
    /// End timestamp (null if not ended)
    /// 结束时间戳（未结束为 null）
    /// </summary>
    DateTime? EndTime { get; }

    /// <summary>
    /// Duration (null if not ended)
    /// 持续时间（未结束为 null）
    /// </summary>
    TimeSpan? Duration { get; }

    /// <summary>
    /// Span status
    /// Span 状态
    /// </summary>
    SpanStatusCode Status { get; }

    /// <summary>
    /// Status description
    /// 状态描述
    /// </summary>
    string? StatusDescription { get; }

    /// <summary>
    /// Span attributes
    /// Span 属性
    /// </summary>
    IReadOnlyDictionary<string, object> Attributes { get; }

    /// <summary>
    /// Span events
    /// Span 事件
    /// </summary>
    IReadOnlyList<TraceEvent> Events { get; }

    /// <summary>
    /// Set attribute
    /// 设置属性
    /// </summary>
    ISpan SetAttribute(string key, object value);

    /// <summary>
    /// Add event
    /// 添加事件
    /// </summary>
    ISpan AddEvent(string name, DateTime? timestamp = null, Dictionary<string, object>? attributes = null);

    /// <summary>
    /// Record exception
    /// 记录异常
    /// </summary>
    ISpan RecordException(global::System.Exception exception, Dictionary<string, object>? attributes = null);

    /// <summary>
    /// Set status
    /// 设置状态
    /// </summary>
    ISpan SetStatus(SpanStatusCode status, string? description = null);

    /// <summary>
    /// End the span
    /// 结束 Span
    /// </summary>
    void End(DateTime? endTime = null);

    /// <summary>
    /// Check if span has ended
    /// 检查 Span 是否已结束
    /// </summary>
    bool HasEnded { get; }
}

/// <summary>
/// Trace event
/// 追踪事件
/// </summary>
public class TraceEvent
{
    /// <summary>
    /// Event name
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Event timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Event attributes
    /// </summary>
    public Dictionary<string, object> Attributes { get; init; } = new();
}

/// <summary>
/// Tracer interface
/// 追踪器接口
/// 
/// 参考: OpenTelemetry Tracer 概念
/// </summary>
public interface ITracer
{
    /// <summary>
    /// Tracer name
    /// 追踪器名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Start a new span
    /// 开始新 Span
    /// </summary>
    ISpan StartSpan(
        string name,
        SpanKind kind = SpanKind.Internal,
        ISpan? parentSpan = null,
        Dictionary<string, object>? attributes = null);

    /// <summary>
    /// Start a new span with explicit trace and parent IDs
    /// 使用显式 trace 和 parent ID 开始新 Span
    /// </summary>
    ISpan StartSpan(
        string name,
        string traceId,
        string? parentSpanId,
        SpanKind kind = SpanKind.Internal,
        Dictionary<string, object>? attributes = null);

    /// <summary>
    /// Get current span from context
    /// 从上下文获取当前 Span
    /// </summary>
    ISpan? CurrentSpan { get; }

    /// <summary>
    /// Create a new trace context
    /// 创建新追踪上下文
    /// </summary>
    TraceContext CreateContext();
}

/// <summary>
/// Trace context for propagation
/// 用于传播的追踪上下文
/// </summary>
public class TraceContext
{
    /// <summary>
    /// Trace ID
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    /// Span ID
    /// </summary>
    public required string SpanId { get; init; }

    /// <summary>
    /// Whether the trace is sampled
    /// 是否采样
    /// </summary>
    public bool IsSampled { get; set; } = true;

    /// <summary>
    /// Trace state for vendor-specific data
    /// 供应商特定的追踪状态
    /// </summary>
    public string? TraceState { get; init; }

    /// <summary>
    /// Serialize to W3C traceparent format
    /// 序列化为 W3C traceparent 格式
    /// </summary>
    public string ToTraceParent()
    {
        var flags = IsSampled ? "01" : "00";
        return $"00-{TraceId}-{SpanId}-{flags}";
    }

    /// <summary>
    /// Parse from W3C traceparent format
    /// 从 W3C traceparent 格式解析
    /// </summary>
    public static TraceContext? FromTraceParent(string traceParent)
    {
        var parts = traceParent.Split('-');
        if (parts.Length < 4) return null;

        return new TraceContext
        {
            TraceId = parts[1],
            SpanId = parts[2],
            IsSampled = parts[3] == "01"
        };
    }
}
