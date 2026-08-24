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

namespace AgentScope.Harness.Skill.Curator;

/// <summary>
/// Skill candidate package submitted to the PromotionGate for review.
/// Skill 候选包，递交给 PromotionGate 审核。
/// </summary>
/// <param name="SkillId">The skill identifier / 技能标识符。</param>
/// <param name="Content">The skill content / 技能内容。</param>
/// <param name="SupportingFiles">Optional supporting files / 可选的附属文件。</param>
/// <param name="SecurityScan">Optional security scan result / 可选的安全扫描结果。</param>
public sealed record SkillCandidate(
    string SkillId,
    string Content,
    Dictionary<string, string>? SupportingFiles = null,
    ScanResult? SecurityScan = null);

/// <summary>
/// Result of a security scan on a skill candidate.
/// 技能候选包的安全扫描结果。
/// </summary>
/// <param name="Verdict">The scan verdict (SAFE/CAUTION/DANGEROUS) / 扫描结论。</param>
/// <param name="Findings">List of security findings / 安全问题列表。</param>
/// <param name="Summary">Optional summary text / 可选的摘要文本。</param>
public sealed record ScanResult(
    string Verdict,
    List<ScanFinding> Findings,
    string? Summary = null);

/// <summary>
/// A single security finding from the scan.
/// 扫描发现的单个安全问题。
/// </summary>
/// <param name="Category">Finding category (e.g. exfiltration, injection) / 问题类别。</param>
/// <param name="Severity">Severity level / 严重级别。</param>
/// <param name="Message">Description of the finding / 问题描述。</param>
/// <param name="LineNumber">Optional source line number / 可选的源代码行号。</param>
public sealed record ScanFinding(
    string Category,
    string Severity,
    string Message,
    int? LineNumber = null);
