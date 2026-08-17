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
using System.Threading;
using System.Threading.Tasks;

namespace AgentScope.Core.Model;

/// <summary>
/// Utility methods for model invocation: executes model calls with timeout/retry logic
/// and normalizes exceptions into ModelException.
/// Corresponds to Java: io.agentscope.core.model.ModelUtils
/// 模型调用工具方法：带超时/重试地执行模型调用，并把异常归一为 ModelException。
/// 对应 Java: io.agentscope.core.model.ModelUtils
/// </summary>
public static class ModelUtils
{
    /// <summary>
    /// Executes a model invocation with retry and timeout strategy.
    /// Retries up to maxAttempts times with configurable delay between attempts.
    /// Timeout is enforced via CancellationTokenSource.
    /// 在重试与超时策略下执行一次模型调用。
    /// 最多重试 maxAttempts 次，每次重试之间可配置延迟。
    /// 通过 CancellationTokenSource 强制执行超时。
    /// </summary>
    /// <typeparam name="T">Return type of the action / 操作的返回类型。</typeparam>
    /// <param name="action">The async action to execute / 要执行的异步操作。</param>
    /// <param name="modelName">Model name for error reporting / 用于错误报告的模型名称。</param>
    /// <param name="provider">Optional provider name / 可选的提供程序名称。</param>
    /// <param name="maxAttempts">Maximum number of retry attempts (default 3) / 最大重试次数（默认 3）。</param>
    /// <param name="timeout">Optional per-attempt timeout / 可选的每次尝试超时时间。</param>
    /// <param name="retryDelay">Delay between retry attempts (default 500ms) / 重试之间的延迟（默认 500ms）。</param>
    /// <param name="cancellationToken">Cancellation token / 取消令牌。</param>
    /// <returns>The result of the action / 操作的结果。</returns>
    /// <exception cref="ModelException">Thrown when all retry attempts fail / 当所有重试尝试都失败时抛出。</exception>
    public static async Task<T> InvokeWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> action,
        string modelName,
        string? provider = null,
        int maxAttempts = 3,
        TimeSpan? timeout = null,
        TimeSpan? retryDelay = null,
        CancellationToken cancellationToken = default)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));

        var delay = retryDelay ?? TimeSpan.FromMilliseconds(500);
        System.Exception? last = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                if (timeout is { } t)
                {
                    cts.CancelAfter(t);
                }

                return await action(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (System.Exception ex)
            {
                last = ex;
                // Timeout (non-external cancellation) or retryable exception
                // 超时（非外部取消）或可重试异常
                if (attempt >= maxAttempts)
                {
                    break;
                }

                try
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }
        }

        throw new ModelException(
            $"模型调用失败（{modelName}），已重试 {maxAttempts} 次。/ Model invocation failed ({modelName}), retried {maxAttempts} times.",
            last!,
            modelName,
            provider ?? "");
    }

    /// <summary>
    /// Determines whether an HTTP status code is retryable (5xx server errors or 429 rate limit).
    /// 判断 HTTP 状态码是否可重试（5xx 服务器错误或 429 速率限制）。
    /// </summary>
    /// <param name="statusCode">HTTP status code / HTTP 状态码。</param>
    /// <returns>True if the status code indicates a retryable error / 如果状态码表示可重试错误则返回 true。</returns>
    public static bool IsRetryableStatus(int statusCode) =>
        statusCode == 429 || (statusCode >= 500 && statusCode <= 599);
}
