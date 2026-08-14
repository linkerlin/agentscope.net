// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace AgentScope.Core.Tracing;

/// <summary>
/// 全局追踪器注册表：按名称注册/查找 ITracer，并提供全局默认追踪器。
/// 对应 Java: io.agentscope.core.tracing.TracerRegistry
/// </summary>
public static class TracerRegistry
{
    private static readonly ConcurrentDictionary<string, ITracer> _tracers = new();
    private static ITracer _global = new NoopTracer();

    /// <summary>全局默认追踪器（默认为 NoopTracer，开销为零）。</summary>
    public static ITracer Global
    {
        get => _global;
        set => _global = value ?? new NoopTracer();
    }

    /// <summary>注册一个命名追踪器。</summary>
    public static void Register(string name, ITracer tracer)
    {
        _tracers[name] = tracer;
    }

    /// <summary>按名称查找追踪器；不存在返回 null。</summary>
    public static ITracer? Get(string name) =>
        _tracers.TryGetValue(name, out var t) ? t : null;

    /// <summary>按名称查找追踪器；不存在返回全局默认。</summary>
    public static ITracer GetOrDefault(string name) =>
        _tracers.TryGetValue(name, out var t) ? t : _global;

    /// <summary>清空所有已注册追踪器（测试用）。</summary>
    public static void Clear() => _tracers.Clear();
}

/// <summary>
/// 空操作追踪器：所有 span 创建即结束，无任何导出开销。适合关闭追踪或测试桩。
/// 对应 Java: io.agentscope.core.tracing.NoopTracer
/// </summary>
public class NoopTracer : ITracer
{
    public static readonly NoopTracer Instance = new();

    public string Name => "noop";

    public ISpan? CurrentSpan => null;

    public ISpan StartSpan(string name, SpanKind kind = SpanKind.Internal, ISpan? parentSpan = null,
        Dictionary<string, object>? attributes = null) => NoopSpan.Instance;

    public ISpan StartSpan(string name, string traceId, string? parentSpanId, SpanKind kind = SpanKind.Internal,
        Dictionary<string, object>? attributes = null) => NoopSpan.Instance;

    public TraceContext CreateContext() => new()
    {
        TraceId = "00000000000000000000000000000000",
        SpanId = "0000000000000000",
        IsSampled = false
    };
}

/// <summary>空操作 Span：所有方法均为 no-op。</summary>
internal sealed class NoopSpan : ISpan
{
    public static readonly NoopSpan Instance = new();

    public string SpanId => "0000000000000000";
    public string TraceId => "00000000000000000000000000000000";
    public string? ParentSpanId => null;
    public string Name => "noop";
    public SpanKind Kind => SpanKind.Internal;
    public DateTime StartTime => DateTime.MinValue;
    public DateTime? EndTime => null;
    public TimeSpan? Duration => null;
    public SpanStatusCode Status => SpanStatusCode.Unset;
    public string? StatusDescription => null;
    public IReadOnlyDictionary<string, object> Attributes => EmptyAttributes;
    public IReadOnlyList<TraceEvent> Events => EmptyEvents;
    public bool HasEnded => true;

    private static readonly IReadOnlyDictionary<string, object> EmptyAttributes =
        new Dictionary<string, object>();
    private static readonly IReadOnlyList<TraceEvent> EmptyEvents =
        new List<TraceEvent>();

    public ISpan SetAttribute(string key, object value) => this;
    public ISpan AddEvent(string name, DateTime? timestamp = null, Dictionary<string, object>? attributes = null) => this;
    public ISpan RecordException(global::System.Exception exception, Dictionary<string, object>? attributes = null) => this;
    public ISpan SetStatus(SpanStatusCode status, string? description = null) => this;
    public void End(DateTime? endTime = null) { }
    public void Dispose() { }
}
