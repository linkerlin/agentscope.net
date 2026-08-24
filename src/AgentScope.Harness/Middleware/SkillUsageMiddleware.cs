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
/// 技能使用埋点中间件：在行动阶段统计模型对技能类工具的查看/使用次数，
/// 为 <see cref="SkillCurator"/> 的 Draft→Active→Stale→Archived 生命周期提供依据。
/// <para>对标 Java: io.agentscope.harness.agent.middleware.SkillUsageMiddleware</para>
/// <para>
/// 语义为"模型本轮决定调用"而非"调用成功"，因此在 <c>next()</c> <b>之前</b>记账。
/// 只对 agent 自建技能真正计数的过滤逻辑位于 Store 内部，中间件不做判断。
/// </para>
/// </summary>
public sealed class SkillUsageMiddleware(SkillUsageStore usageStore) : IHarnessMiddleware
{
    /// <summary>视为"查看技能"的工具名。对标 Java <c>VIEW_TOOL_NAMES</c>。</summary>
    private static readonly HashSet<string> ViewToolNames =
        new(StringComparer.Ordinal) { "load_skill_through_path", "read_skill" };

    /// <summary>视为"使用技能"的工具名。对标 Java <c>USE_TOOL_NAMES</c>。</summary>
    private static readonly HashSet<string> UseToolNames =
        new(StringComparer.Ordinal) { "use_skill" };

    private readonly SkillUsageStore _usageStore =
        usageStore ?? throw new ArgumentNullException(nameof(usageStore));

    public int Order => 760;

    public ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
    {
        foreach (var call in ctx.ToolCalls)
        {
            var isView = ViewToolNames.Contains(call.Name);
            var isUse = UseToolNames.Contains(call.Name);
            if (!isView && !isUse) continue;

            var skillName = ExtractSkillName(call.Input);
            if (string.IsNullOrEmpty(skillName)) continue;

            try
            {
                if (isView) _usageStore.BumpView(skillName);
                else _usageStore.BumpUse(skillName);
            }
            catch (Exception ex)
            {
                // 遥测失败绝不能打断 Agent 循环
                Console.Error.WriteLine($"技能使用埋点失败 [{skillName}]: {ex.Message}");
            }
        }

        return next();
    }

    /// <summary>按 skillId → skill_id → name 的顺序提取技能标识。</summary>
    private static string? ExtractSkillName(Dictionary<string, object>? input)
    {
        if (input == null) return null;
        foreach (var key in new[] { "skillId", "skill_id", "name" })
        {
            if (!input.TryGetValue(key, out var v) || v == null) continue;
            var s = v.ToString()?.Trim();
            if (!string.IsNullOrEmpty(s)) return s;
        }
        return null;
    }
}
