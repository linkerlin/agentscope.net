using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>渠道绑定规则，对�?Java ChannelBinding</summary>
public sealed record ChannelBinding
{
    public string AgentId { get; init; } = "";
    public string? Peer { get; init; }
    public string? ParentPeer { get; init; }
    public string? Guild { get; init; }
    public string? Team { get; init; }
    public string? Account { get; init; }
    public string? Channel { get; init; }
    public IReadOnlySet<string> Roles { get; init; } = new HashSet<string>();
    public DmScope? SessionScope { get; init; }

    public static ChannelBinding ForPeer(string peer, string agentId) =>
        new() { Peer = peer, AgentId = agentId };

    public static ChannelBinding ForGuild(string guild, string agentId) =>
        new() { Guild = guild, AgentId = agentId };

    public static ChannelBinding ForTeam(string team, string agentId) =>
        new() { Team = team, AgentId = agentId };

    public static ChannelBinding ForAccount(string account, string agentId) =>
        new() { Account = account, AgentId = agentId };

    public static ChannelBinding ForChannel(string channel, string agentId) =>
        new() { Channel = channel, AgentId = agentId };
}

