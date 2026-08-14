namespace AgentScope.Harness.Skill.Curator;

/// <summary>Skill 候选包，递交给 PromotionGate 审核，对应 Java SkillCandidate</summary>
public sealed record SkillCandidate(
    string SkillId,
    string Content,
    Dictionary<string, string>? SupportingFiles = null,
    ScanResult? SecurityScan = null);

public sealed record ScanResult(
    string Verdict,
    List<ScanFinding> Findings,
    string? Summary = null);

public sealed record ScanFinding(
    string Category,
    string Severity,
    string Message,
    int? LineNumber = null);
