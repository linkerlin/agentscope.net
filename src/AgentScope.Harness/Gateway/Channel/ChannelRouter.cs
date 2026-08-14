using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>消息渠道路由器，对应 Java ChannelRouter</summary>
public sealed class ChannelRouter
{
    private readonly string _globalDefaultAgentId;

    public ChannelRouter(string globalDefaultAgentId)
    {
        _globalDefaultAgentId = globalDefaultAgentId;
    }

    /// <summary>�?8 层优先级评估路由绑定，返回匹配结�?/summary>
    public RouteResult ResolveRoute(ChannelConfig config, InboundMessage msg)
    {
        var bindings = config.Bindings;
        var peer = msg.Peer;
        var parentPeer = msg.ParentPeer;

        // 按优先级从高到低评估绑定
        foreach (var b in bindings)
        {
            if (b.Peer != null && b.Peer == peer.Id)
                return CreateResult(b.AgentId, "peer", config, msg);
            if (b.ParentPeer != null && parentPeer != null && b.ParentPeer == parentPeer.Id)
                return CreateResult(b.AgentId, "parentPeer", config, msg);
            if (b.Guild != null && msg.Guild != null && b.Guild == msg.Guild && b.Roles.Count > 0)
                return CreateResult(b.AgentId, "guild+roles", config, msg);
            if (b.Guild != null && msg.Guild != null && b.Guild == msg.Guild)
                return CreateResult(b.AgentId, "guild", config, msg);
            if (b.Team != null && msg.Team != null && b.Team == msg.Team)
                return CreateResult(b.AgentId, "team", config, msg);
            if (b.Account != null && msg.AccountId != null && b.Account == msg.AccountId)
                return CreateResult(b.AgentId, "account", config, msg);
        }

        // 回退：使用默�?agent
        var agentId = msg.PreferredAgentId ?? config.DefaultAgentId;
        if (string.IsNullOrEmpty(agentId)) agentId = _globalDefaultAgentId;
        return CreateResult(agentId, "default", config, msg);
    }

    private static RouteResult CreateResult(string agentId, string matchedBy,
        ChannelConfig config, InboundMessage msg)
    {
        var outbound = OutboundAddress.Direct(config.ChannelId,
            msg.Peer.Kind == PeerKind.Direct ? msg.Peer.Id : msg.ChannelId);
        return new RouteResult(agentId, matchedBy, outbound);
    }
}

