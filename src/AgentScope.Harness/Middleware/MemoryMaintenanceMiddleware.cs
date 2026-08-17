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

using System.Globalization;
using AgentScope.Harness.Coordination;
using AgentScope.Harness.Memory;
using AgentScope.Harness.Workspace;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// Memory maintenance middleware that throttles memory housekeeping after each agent call.
/// 记忆维护中间件，在每次 Agent 调用完成后按最小间隔节流地执行记忆维护。
/// </summary>
public sealed class MemoryMaintenanceMiddleware(
    WorkspaceManager workspaceManager,
    MemoryConsolidator? consolidator = null,
    int dailyFileRetentionDays = 90,
    int sessionRetentionDays = 180,
    TimeSpan? minGap = null,
    IsolationScope isolationScope = IsolationScope.User,
    IPeriodicGate? periodicGate = null) : IHarnessMiddleware
{
    /// <summary>
    /// Default minimum interval between maintenance runs.
    /// 两次维护之间的默认最小间隔。
    /// </summary>
    public static readonly TimeSpan DefaultMinGap = TimeSpan.FromMinutes(30);

    private readonly TimeSpan _minGap = minGap ?? DefaultMinGap;
    private readonly IPeriodicGate _gate = periodicGate ?? new LocalPeriodicGate();

    public int Order => 900;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
    {
        await next().ConfigureAwait(false);

        // 维护在主流程之后执行，且任何失败都不得影响本轮结果
        try
        {
            await MaybeRunMaintenanceAsync(ctx, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"记忆维护失败: {ex.Message}");
        }
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx, Func<ValueTask> next, CancellationToken ct = default)
        => next();

    private async Task MaybeRunMaintenanceAsync(MiddlewareContext ctx, CancellationToken ct)
    {
        if (!_gate.TryClaim(CompositeTimerKey(ctx), _minGap)) return;

        ExpireDailyFiles();
        await ConsolidateMemoryAsync(ct).ConfigureAwait(false);
        PruneOldSessions();
    }

    /// <summary>隔离作用域名 + 该作用域下的身份，保证不同维度的节流窗口互不串扰。</summary>
    private string CompositeTimerKey(MiddlewareContext ctx) => $"{isolationScope}:{TimerKeyFor(ctx)}";

    internal string TimerKeyFor(MiddlewareContext ctx) => isolationScope switch
    {
        IsolationScope.User => string.IsNullOrWhiteSpace(ctx.UserId) ? "" : ctx.UserId,
        IsolationScope.Session => string.IsNullOrWhiteSpace(ctx.SessionId) ? "" : ctx.SessionId,
        _ => ""
    };

    /// <summary>把文件名形如 <c>yyyy-MM-dd.md</c> 且早于保留期的日记忆文件移动到 archive/。</summary>
    private void ExpireDailyFiles()
    {
        var files = workspaceManager.ListFiles(WorkspaceConstants.MemoryDir, "*.md");
        if (files.Count == 0) return;

        var cutoff = DateTime.Today.AddDays(-dailyFileRetentionDays);
        foreach (var relPath in files)
        {
            var fileName = Path.GetFileName(relPath);
            if (fileName.StartsWith('.')) continue;

            var baseName = fileName.EndsWith(".md", StringComparison.Ordinal)
                ? fileName[..^3]
                : fileName;

            // 非日期命名的文件（如 MEMORY.md 片段）直接跳过
            if (!DateTime.TryParseExact(baseName, "yyyy-MM-dd",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var fileDate))
                continue;

            if (fileDate >= cutoff) continue;

            try
            {
                workspaceManager.Move(
                    $"{WorkspaceConstants.MemoryDir}/{fileName}",
                    $"{WorkspaceConstants.MemoryArchiveDir}/{fileName}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"归档过期日记忆文件失败 {fileName}: {ex.Message}");
            }
        }
    }

    private async Task ConsolidateMemoryAsync(CancellationToken ct)
    {
        if (consolidator == null) return;
        try
        {
            await consolidator.ConsolidateAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"记忆整合失败: {ex.Message}");
        }
    }

    /// <summary>删除 agents/ 下超过保留期的会话日志（*.log.jsonl）。</summary>
    private void PruneOldSessions()
    {
        var files = workspaceManager.ListFiles(WorkspaceConstants.AgentsDir, "*" + WorkspaceConstants.SessionLogExt);
        if (files.Count == 0) return;

        var cutoff = DateTime.UtcNow.AddDays(-sessionRetentionDays);
        foreach (var relPath in files)
        {
            try
            {
                var modified = workspaceManager.GetLastWriteTimeUtc(relPath);
                if (modified == null || modified >= cutoff) continue;
                workspaceManager.Delete(relPath);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"清理旧会话日志失败 {relPath}: {ex.Message}");
            }
        }
    }
}
