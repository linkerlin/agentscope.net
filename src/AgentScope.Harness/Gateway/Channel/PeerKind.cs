using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>对等体类型，对应 Java PeerKind</summary>
public enum PeerKind
{
    Direct,
    Channel,
    Group,
    Thread
}

