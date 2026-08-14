using System.Text.Json;

namespace AgentScope.Harness.Skill.Curator;

/// <summary>追加式审计日志，对应 Java SkillAuditLog</summary>
public sealed class SkillAuditLog
{
    private readonly string _logDir;

    public SkillAuditLog(string logDir)
    {
        _logDir = logDir;
        Directory.CreateDirectory(_logDir);
    }

    private string TodayFile => Path.Combine(_logDir,
        $"{DateTime.UtcNow:yyyy-MM-dd}.jsonl");

    public async Task AppendAsync(AuditEntry entry, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(entry);
        await File.AppendAllTextAsync(TodayFile, json + "\n", ct);
    }

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

public sealed record AuditEntry
{
    public string SkillId { get; init; } = "";
    public string Action { get; init; } = "";
    public string? ReviewerId { get; init; }
    public DateTime Timestamp { get; init; }
}
