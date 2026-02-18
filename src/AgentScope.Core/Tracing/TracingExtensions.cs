// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.Tracing;

/// <summary>
/// Tracing extension methods
/// 追踪扩展方法
/// </summary>
public static class TracingExtensions
{
    /// <summary>
    /// Start a span for agent processing
    /// 为 Agent 处理启动 Span
    /// </summary>
    public static ISpan StartAgentSpan(this ITracer tracer, IAgent agent, Msg message, ISpan? parentSpan = null)
    {
        var span = tracer.StartSpan(
            $"agent.{agent.Name}.process",
            SpanKind.Server,
            parentSpan,
            new Dictionary<string, object>
            {
                ["agent.name"] = agent.Name,
                ["message.role"] = message.Role,
                ["message.content_length"] = message.Content?.ToString()?.Length ?? 0
            });

        return span;
    }

    /// <summary>
    /// Start a span for model call
    /// 为模型调用启动 Span
    /// </summary>
    public static ISpan StartModelSpan(this ITracer tracer, string modelName, string operation, ISpan? parentSpan = null)
    {
        return tracer.StartSpan(
            $"model.{modelName}.{operation}",
            SpanKind.Client,
            parentSpan,
            new Dictionary<string, object>
            {
                ["model.name"] = modelName,
                ["model.operation"] = operation
            });
    }

    /// <summary>
    /// Start a span for tool execution
    /// 为工具执行启动 Span
    /// </summary>
    public static ISpan StartToolSpan(this ITracer tracer, string toolName, ISpan? parentSpan = null)
    {
        return tracer.StartSpan(
            $"tool.{toolName}.execute",
            SpanKind.Internal,
            parentSpan,
            new Dictionary<string, object>
            {
                ["tool.name"] = toolName
            });
    }

    /// <summary>
    /// Start a span for memory operation
    /// 为内存操作启动 Span
    /// </summary>
    public static ISpan StartMemorySpan(this ITracer tracer, string operation, ISpan? parentSpan = null)
    {
        return tracer.StartSpan(
            $"memory.{operation}",
            SpanKind.Internal,
            parentSpan,
            new Dictionary<string, object>
            {
                ["memory.operation"] = operation
            });
    }

    /// <summary>
    /// Start a span for pipeline node execution
    /// 为管道节点执行启动 Span
    /// </summary>
    public static ISpan StartPipelineSpan(this ITracer tracer, string nodeName, string nodeType, ISpan? parentSpan = null)
    {
        return tracer.StartSpan(
            $"pipeline.{nodeType}.{nodeName}",
            SpanKind.Internal,
            parentSpan,
            new Dictionary<string, object>
            {
                ["pipeline.node_name"] = nodeName,
                ["pipeline.node_type"] = nodeType
            });
    }

    /// <summary>
    /// Wrap an action in a span
    /// 在 Span 中包装操作
    /// </summary>
    public static void WrapInSpan(this ITracer tracer, string spanName, Action<ISpan> action, SpanKind kind = SpanKind.Internal)
    {
        using var span = tracer.StartSpan(spanName, kind);
        try
        {
            action(span);
            span.SetStatus(SpanStatusCode.Ok);
        }
        catch (global::System.Exception ex)
        {
            span.RecordException(ex);
            span.SetStatus(SpanStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Wrap a function in a span
    /// 在 Span 中包装函数
    /// </summary>
    public static T WrapInSpan<T>(this ITracer tracer, string spanName, Func<ISpan, T> func, SpanKind kind = SpanKind.Internal)
    {
        using var span = tracer.StartSpan(spanName, kind);
        try
        {
            var result = func(span);
            span.SetStatus(SpanStatusCode.Ok);
            return result;
        }
        catch (global::System.Exception ex)
        {
            span.RecordException(ex);
            span.SetStatus(SpanStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Wrap an async function in a span
    /// 在 Span 中包装异步函数
    /// </summary>
    public static async Task<T> WrapInSpanAsync<T>(this ITracer tracer, string spanName, Func<ISpan, Task<T>> func, SpanKind kind = SpanKind.Internal)
    {
        using var span = tracer.StartSpan(spanName, kind);
        try
        {
            var result = await func(span);
            span.SetStatus(SpanStatusCode.Ok);
            return result;
        }
        catch (global::System.Exception ex)
        {
            span.RecordException(ex);
            span.SetStatus(SpanStatusCode.Error, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Add standard HTTP attributes to span
    /// 添加标准 HTTP 属性到 Span
    /// </summary>
    public static ISpan WithHttpAttributes(this ISpan span, string method, string url, int? statusCode = null)
    {
        span.SetAttribute("http.method", method);
        span.SetAttribute("http.url", url);
        if (statusCode.HasValue)
        {
            span.SetAttribute("http.status_code", statusCode.Value);
        }
        return span;
    }

    /// <summary>
    /// Add standard database attributes to span
    /// 添加标准数据库属性到 Span
    /// </summary>
    public static ISpan WithDatabaseAttributes(this ISpan span, string system, string operation, string? table = null)
    {
        span.SetAttribute("db.system", system);
        span.SetAttribute("db.operation", operation);
        if (table != null)
        {
            span.SetAttribute("db.table", table);
        }
        return span;
    }

    /// <summary>
    /// Add error attributes to span
    /// 添加错误属性到 Span
    /// </summary>
    public static ISpan WithErrorAttributes(this ISpan span, string errorType, string errorMessage, bool? retryable = null)
    {
        span.SetAttribute("error.type", errorType);
        span.SetAttribute("error.message", errorMessage);
        if (retryable.HasValue)
        {
            span.SetAttribute("error.retryable", retryable.Value);
        }
        return span;
    }
}

/// <summary>
/// Tracer provider for managing global tracer instance
/// 追踪器提供者，用于管理全局追踪器实例
/// </summary>
public static class TracerProvider
{
    private static ITracer? _globalTracer;
    private static readonly object _lock = new();

    /// <summary>
    /// Get or set the global tracer
    /// 获取或设置全局追踪器
    /// </summary>
    public static ITracer? GlobalTracer
    {
        get => _globalTracer;
        set
        {
            lock (_lock)
            {
                _globalTracer = value;
            }
        }
    }

    /// <summary>
    /// Initialize global tracer
    /// 初始化全局追踪器
    /// </summary>
    public static void Initialize(string name, ISpanExporter? exporter = null, Sampler? sampler = null)
    {
        lock (_lock)
        {
            _globalTracer = new Tracer(name, exporter, sampler);
        }
    }

    /// <summary>
    /// Get global tracer or create a no-op tracer if not initialized
    /// 获取全局追踪器，如果未初始化则创建无操作追踪器
    /// </summary>
    public static ITracer GetTracer()
    {
        return _globalTracer ?? new Tracer("noop", null, new AlwaysOffSampler());
    }

    /// <summary>
    /// Check if global tracer is initialized
    /// 检查全局追踪器是否已初始化
    /// </summary>
    public static bool IsInitialized => _globalTracer != null;
}
