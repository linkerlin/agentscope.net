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
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.Tool;

/// <summary>
/// 工具执行器：在重试、超时与取消策略下执行 ITool。
/// 对应 Java: io.agentscope.core.tool.ToolExecutor
/// </summary>
public class ToolExecutor
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _timeout;
    private readonly TimeSpan _retryDelay;
    private readonly Func<System.Exception, int, bool>? _shouldRetry;

    /// <summary>
    /// 创建工具执行器。
    /// </summary>
    /// <param name="maxAttempts">最大尝试次数（含首次），默认 1（不重试）。</param>
    /// <param name="timeout">单次执行超时；null 表示不强制超时。</param>
    /// <param name="retryDelay">重试间隔，默认 0。</param>
    /// <param name="shouldRetry">判定某异常是否应重试的回调；为 null 则对所有异常重试。</param>
    public ToolExecutor(
        int maxAttempts = 1,
        TimeSpan? timeout = null,
        TimeSpan? retryDelay = null,
        Func<System.Exception, int, bool>? shouldRetry = null)
    {
        if (maxAttempts < 1) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        _maxAttempts = maxAttempts;
        _timeout = timeout ?? Timeout.InfiniteTimeSpan;
        _retryDelay = retryDelay ?? TimeSpan.Zero;
        _shouldRetry = shouldRetry;
    }

    /// <summary>
    /// 执行工具，按配置应用重试/超时。
    /// </summary>
    public async Task<ToolResult> ExecuteAsync(ITool tool, Dictionary<string, object> parameters, CancellationToken cancellationToken = default)
    {
        if (tool == null) throw new ArgumentNullException(nameof(tool));

        System.Exception? lastError = null;

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (_timeout != Timeout.InfiniteTimeSpan)
                {
                    linkedCts.CancelAfter(_timeout);
                }

                // 若工具实现 ICancellableTool，则把超时令牌真正传入工具，使其可中止底层工作；
                // 否则仅用 WaitAsync 停止等待（注意：旧式工具超时后底层仍会继续运行）。
                if (tool is ICancellableTool cancellable)
                {
                    return await cancellable.ExecuteAsync(
                        parameters ?? new Dictionary<string, object>(), linkedCts.Token).ConfigureAwait(false);
                }

                return await tool.ExecuteAsync(parameters ?? new Dictionary<string, object>())
                    .WaitAsync(linkedCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                // 超时触发
                return ToolResult.Fail($"工具 {tool.Name} 执行超时（{_timeout}）。");
            }
            catch (System.Exception ex)
            {
                lastError = ex;
                var retry = _shouldRetry == null || _shouldRetry(ex, attempt);
                if (!retry || attempt >= _maxAttempts)
                {
                    break;
                }

                if (_retryDelay > TimeSpan.Zero)
                {
                    await Task.Delay(_retryDelay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return ToolResult.Fail(lastError?.Message ?? "工具执行失败。");
    }
}
