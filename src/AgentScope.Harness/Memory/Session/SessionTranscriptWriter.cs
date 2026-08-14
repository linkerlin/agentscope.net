using System.Text.Json;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Memory.Session;

/// <summary>会话事务日志写入器：将 Msg 转换为 SessionEntry 并追加到 JSONL</summary>
public sealed class SessionTranscriptWriter : IAsyncDisposable
{
    private readonly string _logDir;
    private readonly string _sessionId;
    private StreamWriter? _writer;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public SessionTranscriptWriter(string logDir, string sessionId)
    {
        _logDir = logDir;
        _sessionId = sessionId;
    }

    public string LogPath => Path.Combine(_logDir, $"{_sessionId}.jsonl");

    private async Task EnsureWriterAsync()
    {
        if (_writer != null) return;
        Directory.CreateDirectory(_logDir);
        _writer = new StreamWriter(LogPath, append: true);
    }

    /// <summary>写入消息条目</summary>
    public async Task WriteMessageAsync(Msg msg, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await EnsureWriterAsync();
            var entry = MessageEntry.FromMsg(msg);
            var json = JsonSerializer.Serialize(entry);
            await _writer!.WriteLineAsync(json);
            await _writer.FlushAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>写入工具调用条目</summary>
    public async Task WriteToolUseAsync(string toolName, string? toolCallId,
        string? arguments, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await EnsureWriterAsync();
            var entry = new ToolUseEntry
            {
                ToolName = toolName,
                ToolCallId = toolCallId,
                Arguments = arguments
            };
            await _writer!.WriteLineAsync(JsonSerializer.Serialize(entry));
            await _writer.FlushAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>写入工具结果条目</summary>
    public async Task WriteToolResultAsync(string? toolCallId,
        string? result, bool isError, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await EnsureWriterAsync();
            var entry = new ToolResultEntry
            {
                ToolCallId = toolCallId,
                Result = result,
                IsError = isError
            };
            await _writer!.WriteLineAsync(JsonSerializer.Serialize(entry));
            await _writer.FlushAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>写入压缩条目</summary>
    public async Task WriteCompactionAsync(string summary,
        int originalCount, int compressedCount, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await EnsureWriterAsync();
            var entry = new CompactionEntry
            {
                Summary = summary,
                OriginalMessageCount = originalCount,
                CompressedMessageCount = compressedCount
            };
            await _writer!.WriteLineAsync(JsonSerializer.Serialize(entry));
            await _writer.FlushAsync();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>读取所有已记录的条目</summary>
    public async Task<List<SessionEntry>> ReadAllAsync(CancellationToken ct = default)
    {
        var entries = new List<SessionEntry>();
        if (!File.Exists(LogPath)) return entries;

        var lines = await File.ReadAllLinesAsync(LogPath, ct);
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
        await _lock.WaitAsync();
        try
        {
            if (_writer != null)
            {
                await _writer.FlushAsync();
                await _writer.DisposeAsync();
                _writer = null;
            }
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }
}
