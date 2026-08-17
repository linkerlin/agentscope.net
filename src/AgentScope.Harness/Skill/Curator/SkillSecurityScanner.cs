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
