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

using System.Collections.Concurrent;
using System.Text.Json;

namespace AgentScope.Harness.Memory.Session;

/// <summary>双文件 JSONL 会话树管理：context + log，支持分段持久化</summary>
public sealed class SessionTree : IAsyncDisposable
{
    private readonly string _baseDir;
    private readonly string _sessionId;
    private readonly ConcurrentQueue<SessionEntry> _pendingEntries = new();
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    private bool _disposed;

    public SessionTree(string baseDir, string sessionId)
    {
        _baseDir = baseDir;
        _sessionId = sessionId;
        Directory.CreateDirectory(baseDir);
    }

    private string ContextPath => Path.Combine(_baseDir, $"{_sessionId}.ctx.jsonl");
    private string LogPath => Path.Combine(_baseDir, $"{_sessionId}.log.jsonl");

    /// <summary>追加条目到待写队列</summary>
    public void Append(SessionEntry entry)
    {
        _pendingEntries.Enqueue(entry);
    }

    /// <summary>刷新待写条目到日志文件</summary>
    public async Task FlushAsync(CancellationToken ct = default)
    {
        await _flushLock.WaitAsync(ct);
        try
        {
            var batch = new List<SessionEntry>();
            while (_pendingEntries.TryDequeue(out var entry))
                batch.Add(entry);

            if (batch.Count == 0) return;

            var lines = batch.Select(e => JsonSerializer.Serialize(e));
            await File.AppendAllLinesAsync(LogPath, lines, ct);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    /// <summary>读取上下文文件中的所有条目</summary>
    public async Task<List<SessionEntry>> LoadContextAsync(CancellationToken ct = default)
    {
        return await LoadFileAsync(ContextPath, ct);
    }

    /// <summary>读取日志文件中的所有条目</summary>
    public async Task<List<SessionEntry>> LoadLogAsync(CancellationToken ct = default)
    {
        return await LoadFileAsync(LogPath, ct);
    }

    /// <summary>保存上下文（覆盖写入）</summary>
    public async Task SaveContextAsync(IEnumerable<SessionEntry> entries,
        CancellationToken ct = default)
    {
        await _flushLock.WaitAsync(ct);
        try
        {
            var lines = entries.Select(e => JsonSerializer.Serialize(e));
            await File.WriteAllLinesAsync(ContextPath, lines, ct);
        }
        finally
        {
            _flushLock.Release();
        }
    }

    /// <summary>获取日志文件大小（字节）</summary>
    public long GetLogSize()
    {
        try { return new FileInfo(LogPath).Length; }
        catch { return 0; }
    }

    private static async Task<List<SessionEntry>> LoadFileAsync(
        string path, CancellationToken ct)
    {
        var entries = new List<SessionEntry>();
        if (!File.Exists(path)) return entries;

        var lines = await File.ReadAllLinesAsync(path, ct);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var type = doc.RootElement.GetProperty("type").GetString();
                SessionEntry? entry = null;
                if (type == "message") entry = JsonSerializer.Deserialize<MessageEntry>(line);
                else if (type == "tool_use") entry = JsonSerializer.Deserialize<ToolUseEntry>(line);
                else if (type == "tool_result") entry = JsonSerializer.Deserialize<ToolResultEntry>(line);
                else if (type == "compaction") entry = JsonSerializer.Deserialize<CompactionEntry>(line);
                else if (type == "summary") entry = JsonSerializer.Deserialize<SummaryEntry>(line);
                if (entry != null) entries.Add(entry);
            }
            catch { }
        }
        return entries;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await FlushAsync();
        _flushLock.Dispose();
        _disposed = true;
    }
}
