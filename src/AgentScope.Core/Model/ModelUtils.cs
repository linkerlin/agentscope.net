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
/// 模型调用工具方法：带超时/重试地执行模型调用，并把异常归一为 ModelException。
/// 对应 Java: io.agentscope.core.model.ModelUtils
/// </summary>
public static class ModelUtils
{
    /// <summary>
    /// 在重试与超时策略下执行一次模型调用。
    /// </summary>
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
            $"模型调用失败（{modelName}），已重试 {maxAttempts} 次。",
            last!,
            modelName,
            provider ?? "");
    }

    /// <summary>判断 HTTP 状态码是否可重试（5xx / 429）。</summary>
    public static bool IsRetryableStatus(int statusCode) =>
        statusCode == 429 || (statusCode >= 500 && statusCode <= 599);
}
