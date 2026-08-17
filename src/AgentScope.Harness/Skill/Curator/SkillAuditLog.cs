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

using System.Text.Json;

namespace AgentScope.Harness.Skill.Curator;

/// <summary>
/// Append-only audit log for skill promotion and curation events.
/// 追加式审计日志，用于技能提升和整理事件记录。
/// </summary>
public sealed class SkillAuditLog
{
    private readonly string _logDir;

    /// <summary>
    /// Initializes a new instance of <see cref="SkillAuditLog"/>.
    /// 初始化 <see cref="SkillAuditLog"/> 的新实例。
    /// </summary>
    /// <param name="logDir">Directory for audit log files / 审计日志文件目录。</param>
    public SkillAuditLog(string logDir)
    {
        _logDir = logDir;
        Directory.CreateDirectory(_logDir);
    }

    private string TodayFile => Path.Combine(_logDir,
        $"{DateTime.UtcNow:yyyy-MM-dd}.jsonl");

    /// <summary>
    /// Appends an audit entry to today's log file.
    /// 将审计条目追加到当天日志文件。
    /// </summary>
    /// <param name="entry">The audit entry to append / 待追加的审计条目。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    public async Task AppendAsync(AuditEntry entry, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(entry);
        await File.AppendAllTextAsync(TodayFile, json + "\n", ct);
    }

    /// <summary>
    /// Queries audit entries for a specific day, optionally filtered by a predicate.
    /// 查询指定日期的审计条目，可通过谓词过滤。
    /// </summary>
    /// <param name="dayUtc">The UTC date string (yyyy-MM-dd) / UTC 日期字符串(yyyy-MM-dd)。</param>
    /// <param name="predicate">Optional filter predicate / 可选的过滤谓词。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    /// <returns>List of matching audit entries / 匹配的审计条目列表。</returns>
    public async Task<List<AuditEntry>> QueryAsync(string dayUtc,
        Func<AuditEntry, bool>? predicate = null, CancellationToken ct = default)
    {
        var path = Path.Combine(_logDir, $"{dayUtc}.jsonl");
        if (!File.Exists(path)) return new();

        var entries = new List<AuditEntry>();
        var lines = await File.ReadAllLinesAsync(path, ct);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var entry = JsonSerializer.Deserialize<AuditEntry>(line);
                if (entry != null && (predicate == null || predicate(entry)))
                    entries.Add(entry);
            }
            catch { }
        }
        return entries;
    }

    /// <summary>
    /// Logs a promotion-related action (approve/reject) for a skill.
    /// 记录技能的提升相关操作（批准/拒绝）。
    /// </summary>
    /// <param name="skillId">The skill ID / 技能 ID。</param>
    /// <param name="action">The action name / 操作名称。</param>
    /// <param name="reviewerId">Optional reviewer identifier / 可选的审核人 ID。</param>
    /// <param name="ct">Cancellation token / 取消令牌。</param>
    public async Task LogPromotionAsync(string skillId, string action,
        string? reviewerId = null, CancellationToken ct = default)
    {
        await AppendAsync(new AuditEntry
        {
            SkillId = skillId,
            Action = action,
            ReviewerId = reviewerId,
            Timestamp = DateTime.UtcNow
        }, ct);
    }
}

/// <summary>
/// Represents a single audit log entry for a skill operation.
/// 表示技能操作的单个审计日志条目。
/// </summary>
public sealed record AuditEntry
{
    /// <summary>
    /// The skill ID / 技能 ID。
    /// </summary>
    public string SkillId { get; init; } = "";
    /// <summary>
    /// The action performed / 执行的操作。
    /// </summary>
    public string Action { get; init; } = "";
    /// <summary>
    /// Optional reviewer identifier / 可选的审核人 ID。
    /// </summary>
    public string? ReviewerId { get; init; }
    /// <summary>
    /// The timestamp of the action / 操作时间戳。
    /// </summary>
    public DateTime Timestamp { get; init; }
}
