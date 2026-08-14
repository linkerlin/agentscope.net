namespace AgentScope.Core.Service.Discovery;

/// <summary>
/// DataPlane 自注册心跳服务。对标 aistio DataPlaneSelfRegistration。
/// 通过 IHostedService 周期性向控制面发送心跳并更新本地注册表。
/// </summary>
public sealed class DataPlaneSelfRegistration(
    DataPlaneRegistry registry,
    IDataPlaneControlPlaneClient controlPlane,
    TimeProvider clock) : IDisposable
{
    private Timer? _timer;

    public void Start()
    {
        _timer = new Timer(_ => _ = HeartbeatAsync(), null, clock.GetUtcNow().ToLocalTime().TimeOfDay, TimeSpan.FromSeconds(15));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private async Task HeartbeatAsync()
    {
        var caps = new Capabilities("agentscope-dotnet", 3, "1.2.0");
        await controlPlane.HeartbeatAsync(caps, CancellationToken.None);
        registry.Upsert(caps.Endpoint,
            new AgentSummary(caps.Runtime, caps.Runtime, caps.Endpoint, caps.ContractLevel.ToString()));
    }

    public void Dispose() => Stop();
}
