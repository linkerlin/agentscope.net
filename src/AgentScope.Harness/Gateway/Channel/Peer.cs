using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>消息对等体，对应 Java Peer</summary>
public sealed record Peer(PeerKind Kind, string Id)
{
    public string Key => $"{Kind.ToString().ToLowerInvariant()}:{Id}";

    public static Peer Direct(string id) => new(PeerKind.Direct, id);
    public static Peer Channel(string id) => new(PeerKind.Channel, id);
    public static Peer Group(string id) => new(PeerKind.Group, id);
    public static Peer Thread(string id) => new(PeerKind.Thread, id);
}

