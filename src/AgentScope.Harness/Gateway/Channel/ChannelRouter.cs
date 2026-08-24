// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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

    /// <summary>按 8 层优先级评估路由绑定，返回匹配结果</summary>
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

