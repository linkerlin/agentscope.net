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
namespace AgentScope.Harness.Skill.Curator;

public sealed class SkillPromoter
{
    private readonly ISkillPromotionGate _gate;
    private readonly SkillSecurityScanner _scanner;
    private readonly SkillAuditLog _auditLog;

    public SkillPromoter(ISkillPromotionGate gate, SkillSecurityScanner scanner, SkillAuditLog auditLog)
    {
        _gate = gate;
        _scanner = scanner;
        _auditLog = auditLog;
    }

    public async Task<PromotionResult> PromoteAsync(string skillId,
        string content, string? reviewerId = null,
        RuntimeContext? context = null, CancellationToken ct = default)
    {
        var candidate = new SkillCandidate(skillId, content);
        var scanResult = _scanner.Scan(skillId, content);

        if (scanResult.Verdict == "DANGEROUS")
            return new PromotionResult(PromotionStatus.Rejected,
                "Security scan failed: dangerous content detected");

        candidate = candidate with { SecurityScan = scanResult };
        var decision = await _gate.ReviewAsync(candidate, context, ct);

        return decision switch
        {
            Approve a => await HandleApprove(skillId, a, ct),
            Reject r => await HandleReject(skillId, r, ct),
            Defer d => new PromotionResult(PromotionStatus.Deferred, d.Reason),
            _ => new PromotionResult(PromotionStatus.Deferred, "Unknown decision")
        };
    }

    private async Task<PromotionResult> HandleApprove(string skillId, Approve a, CancellationToken ct)
    {
        await _auditLog.LogPromotionAsync(skillId, "approved", a.ReviewerId, ct);
        return new PromotionResult(PromotionStatus.Approved, $"Approved by {a.ReviewerId}");
    }

    private async Task<PromotionResult> HandleReject(string skillId, Reject r, CancellationToken ct)
    {
        await _auditLog.LogPromotionAsync(skillId, "rejected", r.ReviewerId, ct);
        return new PromotionResult(PromotionStatus.Rejected, r.Reason);
    }
}

public sealed record PromotionResult(PromotionStatus Status, string? Message = null);
public enum PromotionStatus { Approved, Deferred, Rejected, Invalid }
