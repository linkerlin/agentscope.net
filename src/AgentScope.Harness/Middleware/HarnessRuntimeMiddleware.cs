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

using AgentScope.Core.Agent;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// Harness runtime middleware: injects runtime context (workspace, session, isolation domain)
/// at the start of each turn and refreshes runtime statistics after the turn ends.
/// Harness 运行时中间件：在每个回合开始时注入 Harness 运行时上下文（工作区、会话、隔离域），
/// 在回合结束后刷新运行时统计。
/// </summary>
public class HarnessRuntimeMiddleware : MiddlewareBase
{
    private long _turnCount;
    private long _errorCount;

    /// <summary>
    /// Total accumulated turn count.
    /// 累计回合数。
    /// </summary>
    public long TurnCount => Interlocked.Read(ref _turnCount);

    /// <summary>
    /// Total accumulated error count.
    /// 累计错误数。
    /// </summary>
    public long ErrorCount => Interlocked.Read(ref _errorCount);

    /// <summary>
    /// Increments turn counter, invokes the next middleware, and tracks errors.
    /// 增加回合计数，调用下一中间件，并跟踪错误。
    /// </summary>
    /// <param name="input">Agent input / Agent 输入。</param>
    /// <param name="next">Next delegate / 下一个委托。</param>
    public override async IAsyncEnumerable<Core.Events.Event> OnAgentAsync(
        AgentInput input,
        Func<AgentInput, IAsyncEnumerable<Core.Events.Event>> next)
    {
        Interlocked.Increment(ref _turnCount);
        var enumerator = next(input).GetAsyncEnumerator();
        try
        {
            while (true)
            {
                bool moved;
                try
                {
                    moved = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch
                {
                    Interlocked.Increment(ref _errorCount);
                    throw;
                }

                if (!moved) break;
                yield return enumerator.Current;
            }
        }
        finally
        {
            await enumerator.DisposeAsync().ConfigureAwait(false);
        }
    }
}
