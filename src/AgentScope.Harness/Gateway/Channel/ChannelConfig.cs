using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>渠道路由配置，对�?Java ChannelConfig</summary>
public sealed record ChannelConfig
{
    public string ChannelId { get; init; }
    public string DefaultAgentId { get; init; } = "";
    public DmScope DmScope { get; init; } = DmScope.Main;
    public IReadOnlyList<ChannelBinding> Bindings { get; init; } = [];

    public static ChannelConfig Of(string channelId) => new() { ChannelId = channelId };

    public static ChannelConfig Of(string channelId, string defaultAgentId) =>
        new() { ChannelId = channelId, DefaultAgentId = defaultAgentId };
}

