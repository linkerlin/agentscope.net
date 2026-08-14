using AgentScope.Core.Message;
using AgentScope.Harness.Memory.Session;

namespace AgentScope.Harness.Memory;

/// <summary>记忆刷出管理器：从会话窗口提取记忆并追加到日记录账</summary>
public sealed class MemoryFlushManager
{
    private readonly MemoryConfig _config;
    private readonly SessionTranscriptWriter _writer;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public MemoryFlushManager(MemoryConfig config, SessionTranscriptWriter writer)
    {
        _config = config;
        _writer = writer;
    }

    /// <summary>刷出单条消息到日志</summary>
    public async Task FlushMessageAsync(Msg msg, CancellationToken ct = default)
    {
        await _writer.WriteMessageAsync(msg, ct);
    }

    /// <summary>刷出消息批处理</summary>
    public async Task FlushBatchAsync(IEnumerable<Msg> messages,
        CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            foreach (var msg in messages)
                await _writer.WriteMessageAsync(msg, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>刷出工具调用记录</summary>
    public async Task FlushToolUseAsync(string toolName,
        string? toolCallId, string? arguments, CancellationToken ct = default)
    {
        await _writer.WriteToolUseAsync(toolName, toolCallId, arguments, ct);
    }

    /// <summary>刷出工具结果记录</summary>
    public async Task FlushToolResultAsync(string? toolCallId,
        string? result, bool isError, CancellationToken ct = default)
    {
        await _writer.WriteToolResultAsync(toolCallId, result, isError, ct);
    }
}
