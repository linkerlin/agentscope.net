using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>入站消息，对�?Java InboundMessage</summary>
public sealed record InboundMessage
{
    public string ChannelId { get; init; } = "";
    public string? AccountId { get; init; }
    public Peer Peer { get; init; } = Peer.Direct("");
    public string? SenderId { get; init; }
    public Peer? ParentPeer { get; init; }
    public string? Guild { get; init; }
    public string? Team { get; init; }
    public IReadOnlySet<string> Roles { get; init; } = new HashSet<string>();
    public IReadOnlyList<Msg> Messages { get; init; } = [];
    public string? PreferredAgentId { get; init; }

    public bool IsDm => Peer.Kind == PeerKind.Direct;
    public bool IsThread => Peer.Kind == PeerKind.Thread;
}

