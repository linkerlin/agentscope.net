namespace AgentScope.Core.Service.Discovery;

/// <summary>
/// 数据面→控制面通信接口。对标 aistio DataPlaneClient。
/// </summary>
public interface IDataPlaneControlPlaneClient
{
    ValueTask HeartbeatAsync(Capabilities caps, CancellationToken ct = default);
    ValueTask<DataPlaneConfig> PullConfigAsync(string agentId, CancellationToken ct = default);
}

/// <summary>
/// 能力声明。对标 aistio Capabilities。
/// </summary>
public readonly record struct Capabilities(string Runtime, int ContractLevel, string Version)
{
    public string Endpoint { get; init; } = "";
}

/// <summary>
/// 数据面配置（从控制面拉取）。
/// </summary>
public readonly record struct DataPlaneConfig(
    IReadOnlyList<string> AllowedTools,
    IReadOnlyDictionary<string, string> Variables);
