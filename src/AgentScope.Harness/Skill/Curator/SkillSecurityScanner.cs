using System.Text.RegularExpressions;

namespace AgentScope.Harness.Skill.Curator;

/// <summary>Skill 安全扫描器（正则规则库），对应 Java SkillSecurityScanner</summary>
public sealed class SkillSecurityScanner
{
    private static readonly List<(string Category, string Pattern, Severity Sev)> Rules = new()
    {
        ("exfiltration", @"curl\s+http", Severity.High),
        ("exfiltration", @"wget\s+http", Severity.High),
        ("injection", @"eval\(", Severity.Critical),
        ("injection", @"Process\.Start", Severity.High),
        ("destructive", @"rm\s+-rf", Severity.Critical),
        ("destructive", @"Directory\.Delete", Severity.High),
        ("network", @"socket|HttpClient|WebClient", Severity.Medium),
        ("obfuscation", @"base64.*decode|([A-Za-z0-9+/]{40,})", Severity.Medium),
    };

    public ScanResult Scan(string skillName, string content,
        Dictionary<string, string>? resources = null)
    {
        var findings = new List<ScanFinding>();
        var allContent = content;
        if (resources != null)
            allContent += "\n" + string.Join("\n", resources.Values);

        foreach (var (category, pattern, severity) in Rules)
        {
            var matches = Regex.Matches(allContent, pattern,
                RegexOptions.IgnoreCase | RegexOptions.Multiline);
            foreach (Match m in matches)
            {
                var lineNo = allContent[..m.Index].Count(c => c == '\n') + 1;
                findings.Add(new ScanFinding(category, severity.ToString(),
                    $"发现 {category} 模式: {m.Value}", lineNo));
            }
        }

        var verdict = findings.Any(f => f.Severity == "Critical")
            ? "DANGEROUS"
            : findings.Any(f => f.Severity == "High")
                ? "CAUTION"
                : "SAFE";

        return new ScanResult(verdict, findings,
            findings.Count > 0 ? $"发现 {findings.Count} 个安全问题" : "扫描通过");
    }
}

public enum Severity { Low, Medium, High, Critical }
