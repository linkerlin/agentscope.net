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

using AgentScope.Core.Message;
using AgentScope.Harness.Memory.Session;

namespace AgentScope.Harness.Memory;

/// <summary>
/// Memory flush manager that extracts memory from the session window and appends it to the daily log.<br />
/// 记忆刷出管理器：从会话窗口提取记忆并追加到日记录账。
/// </summary>
public sealed class MemoryFlushManager
{
    private readonly MemoryConfig _config;
    private readonly SessionTranscriptWriter _writer;
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryFlushManager"/> class.<br />
    /// 初始化 <see cref="MemoryFlushManager"/> 的新实例。
    /// </summary>
    /// <param name="config">Memory configuration / 记忆配置</param>
    /// <param name="writer">Session transcript writer / 会话事务日志写入器</param>
    public MemoryFlushManager(MemoryConfig config, SessionTranscriptWriter writer)
    {
        _config = config;
        _writer = writer;
    }

    /// <summary>
    /// Flushes a single message to the daily log.<br />
    /// 刷出单条消息到日志。
    /// </summary>
    /// <param name="msg">Message to flush / 待刷出的消息</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async Task FlushMessageAsync(Msg msg, CancellationToken ct = default)
    {
        await _writer.WriteMessageAsync(msg, ct);
    }

    /// <summary>
    /// Flushes a batch of messages to the daily log.<br />
    /// 批量刷出消息到日志。
    /// </summary>
    /// <param name="messages">Messages to flush / 待刷出的消息集合</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
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

    /// <summary>
    /// Flushes a tool use record to the daily log.<br />
    /// 刷出工具调用记录到日志。
    /// </summary>
    /// <param name="toolName">Name of the tool / 工具名称</param>
    /// <param name="toolCallId">Tool call identifier / 工具调用标识</param>
    /// <param name="arguments">Tool arguments / 工具参数</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async Task FlushToolUseAsync(string toolName,
        string? toolCallId, string? arguments, CancellationToken ct = default)
    {
        await _writer.WriteToolUseAsync(toolName, toolCallId, arguments, ct);
    }

    /// <summary>
    /// Flushes a tool result record to the daily log.<br />
    /// 刷出工具结果记录到日志。
    /// </summary>
    /// <param name="toolCallId">Tool call identifier / 工具调用标识</param>
    /// <param name="result">Tool execution result / 工具执行结果</param>
    /// <param name="isError">Whether the result is an error / 结果是否为错误</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async Task FlushToolResultAsync(string? toolCallId,
        string? result, bool isError, CancellationToken ct = default)
    {
        await _writer.WriteToolResultAsync(toolCallId, result, isError, ct);
    }
}
