using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>出站地址，对�?Java OutboundAddress</summary>
public sealed record OutboundAddress
{
    public string ChannelId { get; init; } = "";
    public string? AccountId { get; init; }
    public string To { get; init; } = "";
    public string? ThreadId { get; init; }

    public static OutboundAddress Direct(string channelId, string to) =>
        new() { ChannelId = channelId, To = to };
}

