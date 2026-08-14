// Copyright 2024-2026 the original author or authors.
//
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.Tracing;

/// <summary>
/// JSONL 格式的 Span 导出器，将 TraceSpan 序列化为 JSON 行写入文件。
/// 参考 OpenTelemetry SpanExporter 概念，适用于日志聚合系统（如 ELK、Datadog）。
/// </summary>
public class JsonlTraceExporter : ISpanExporter
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// 创建 JSONL 导出器
    /// </summary>
    /// <param name="filePath">输出文件路径</param>
    public JsonlTraceExporter(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));

        // 确保目录存在
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <inheritdoc />
    public void Export(TraceSpan span)
    {
        if (span == null) return;

        var entry = SerializeSpan(span);
        var json = JsonSerializer.Serialize(entry, _jsonOptions);

        lock (_lock)
        {
            File.AppendAllText(_filePath, json + Environment.NewLine);
        }
    }

    /// <inheritdoc />
    public void ExportBatch(IEnumerable<TraceSpan> spans)
    {
        if (spans == null) return;

        var lines = new List<string>();
        foreach (var span in spans)
        {
            if (span == null) continue;
            var entry = SerializeSpan(span);
            lines.Add(JsonSerializer.Serialize(entry, _jsonOptions));
        }

        if (lines.Count == 0) return;

        lock (_lock)
        {
            File.AppendAllLines(_filePath, lines);
        }
    }

    /// <inheritdoc />
    public Task FlushAsync(CancellationToken ct = default)
    {
        // File.AppendAllText/AppendAllLines 已同步写入，无需额外 flush
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// 将 TraceSpan 序列化为字典，便于 JSON 输出
    /// </summary>
    private static Dictionary<string, object?> SerializeSpan(TraceSpan span)
    {
        var entry = new Dictionary<string, object?>
        {
            ["traceId"] = span.TraceId,
            ["spanId"] = span.SpanId,
            ["parentSpanId"] = span.ParentSpanId,
            ["name"] = span.Name,
            ["kind"] = span.Kind.ToString(),
            ["status"] = span.Status.ToString(),
            ["statusDescription"] = span.StatusDescription,
            ["startTime"] = span.StartTime.ToString("O"),
            ["endTime"] = span.EndTime?.ToString("O"),
            ["durationMs"] = span.Duration?.TotalMilliseconds,
            ["attributes"] = span.Attributes.Count > 0 ? span.Attributes.ToDictionary(kv => kv.Key, kv => kv.Value) : null,
            ["events"] = span.Events.Count > 0
                ? span.Events.Select(e => new Dictionary<string, object?>
                {
                    ["name"] = e.Name,
                    ["timestamp"] = e.Timestamp.ToString("O"),
                    ["attributes"] = e.Attributes.Count > 0 ? e.Attributes : null
                }).ToList()
                : null
        };

        return entry;
    }
}
