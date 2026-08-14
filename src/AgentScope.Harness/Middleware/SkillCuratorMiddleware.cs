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

using AgentScope.Harness.Skill.Curator;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 技能策展中间件：在 Agent 回合<b>正常完成</b>后，异步触发一次技能生命周期策展。
/// <para>对标 Java: io.agentscope.harness.agent.middleware.SkillCuratorMiddleware</para>
/// <para>
/// 与 Java 一致：仅在主流程无异常完成时触发（异常路径不触发）；
/// 是否真正执行由 <see cref="SkillCurator.ShouldRunNow"/> 的间隔判定决定；
/// 执行始终在后台任务中进行，绝不阻塞 Agent 循环。
/// </para>
/// </summary>
public sealed class SkillCuratorMiddleware(SkillCurator curator) : IHarnessMiddleware, IDisposable
{
    private readonly SkillCurator _curator = curator ?? throw new ArgumentNullException(nameof(curator));
    private volatile bool _shutdown;

    /// <summary>暴露内部策展器，供 <c>agent.RunCuratorOnce()</c> 之类的手动触发使用。</summary>
    public SkillCurator Curator => _curator;

    public int Order => 780;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
    {
        await next().ConfigureAwait(false);
        // 到达此处即表示主流程正常完成（异常会向上抛出，不触发策展）
        MaybeRunCurator();
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    private void MaybeRunCurator()
    {
        if (_shutdown) return;

        var now = DateTime.UtcNow;
        try
        {
            if (!_curator.ShouldRunNow(now)) return;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"技能策展间隔判定失败: {ex.Message}");
            return;
        }

        _ = Task.Run(() =>
        {
            if (_shutdown) return;
            try
            {
                var report = _curator.RunOnce(DateTime.UtcNow);
                var c = report.Counters;
                Console.WriteLine(
                    $"技能策展完成: active={c.ActiveCount}, stale={c.StaleCount}, " +
                    $"archived={c.ArchivedCount}, durationSec={report.DurationSeconds}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"技能策展执行失败: {ex.Message}");
            }
        });
    }

    /// <summary>停止后续策展触发（幂等）。</summary>
    public void Dispose() => _shutdown = true;
}
