namespace AgentScope.Harness.Memory.Session;

/// <summary>会话新鲜度评估器：判断会话是否过期或应重置</summary>
public sealed class SessionFreshnessEvaluator
{
    private readonly TimeSpan _idleTimeout;
    private readonly bool _resetDaily;

    public SessionFreshnessEvaluator(
        TimeSpan? idleTimeout = null,
        bool resetDaily = true)
    {
        _idleTimeout = idleTimeout ?? TimeSpan.FromHours(24);
        _resetDaily = resetDaily;
    }

    /// <summary>判断会话是否因空闲超时而过期</summary>
    public bool IsIdleExpired(DateTime lastActivityTime)
    {
        return DateTime.UtcNow - lastActivityTime > _idleTimeout;
    }

    /// <summary>判断会话是否需要每日重置</summary>
    public bool ShouldResetDaily(DateTime lastActivityTime)
    {
        if (!_resetDaily) return false;
        return lastActivityTime.Date < DateTime.UtcNow.Date;
    }

    /// <summary>获取下次重置时间</summary>
    public DateTime GetNextResetTime() =>
        DateTime.UtcNow.Date.AddDays(1);
}
