using AgentScope.Core.Agent;
namespace AgentScope.Harness.Skill.Curator;

public interface ISkillPromotionGate
{
    Task<PromotionDecision> ReviewAsync(SkillCandidate candidate,
        RuntimeContext? context, CancellationToken ct = default);
}

public abstract record PromotionDecision;
public sealed record Approve(string ReviewerId, DateTime DecidedAt) : PromotionDecision;
public sealed record Reject(string Reason, string? ReviewerId = null) : PromotionDecision;
public sealed record Defer(string Reason, TimeSpan? RetryAfter = null) : PromotionDecision;

public sealed class LocalApprovalGate : ISkillPromotionGate
{
    private readonly Func<string, Task<bool>> _prompter;

    public LocalApprovalGate(Func<string, Task<bool>>? prompter = null)
    {
        _prompter = prompter ?? (async msg =>
        {
            Console.Write(msg);
            var input = await Task.Run(() => Console.ReadLine());
            return input?.Trim().ToLower() == "y";
        });
    }

    public async Task<PromotionDecision> ReviewAsync(SkillCandidate candidate,
        RuntimeContext? context, CancellationToken ct = default)
    {
        var approved = await _prompter($"Approve skill '{candidate.SkillId}' for promotion? (y/N): ");
        return approved ? new Approve("local", DateTime.UtcNow) : new Defer("Waiting for user confirmation");
    }
}

public sealed class RejectAllGate : ISkillPromotionGate
{
    public Task<PromotionDecision> ReviewAsync(SkillCandidate candidate,
        RuntimeContext? context, CancellationToken ct = default)
        => Task.FromResult<PromotionDecision>(new Defer("Configured for manual review"));
}

public sealed class NotifyAndWaitGate : ISkillPromotionGate
{
    private readonly List<INotificationSink> _sinks;
    public NotifyAndWaitGate(IEnumerable<INotificationSink> sinks) => _sinks = new(sinks);

    public async Task<PromotionDecision> ReviewAsync(SkillCandidate candidate,
        RuntimeContext? context, CancellationToken ct = default)
    {
        foreach (var sink in _sinks)
            await sink.NotifyAsync(candidate, ct);
        return new Defer("Review notification sent, waiting for external approval");
    }
}

public interface INotificationSink
{
    Task NotifyAsync(SkillCandidate candidate, CancellationToken ct = default);
}
