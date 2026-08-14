using AgentScope.Core.AgUI.Model;

namespace AgentScope.Core.AgUI.Adapter;

/// <summary>
/// AG-UI 适配器配置。对标 Java AguiAdapterConfig。
/// </summary>
public sealed record AguiAdapterConfig
{
    public ToolMergeMode ToolMergeMode { get; init; } = ToolMergeMode.FrontendOnly;
    public bool EmitStateEvents { get; init; }
    public bool EmitToolCallArgs { get; init; } = true;
    public bool EmitTokenUsage { get; init; }
    public bool EnableReasoning { get; init; } = true;
    public bool EmitRunFinishedAfterError { get; init; } = true;
    public TimeSpan? RunTimeout { get; init; }
    public string DefaultAgentId { get; init; } = "default";
    public bool EmitSubagentEventsAsNative { get; init; }
}
