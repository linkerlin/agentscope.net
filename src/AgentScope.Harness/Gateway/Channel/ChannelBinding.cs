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

/// <summary>
/// 渠道绑定规则，将消息源（Peer/Guild/Team/Account 等）映射到目标 Agent。
/// Channel binding rule that maps message sources (Peer/Guild/Team/Account etc.) to a target agent.
/// 对应 Java ChannelBinding。
/// </summary>
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

    /// <summary>为指定对等体创建绑定 / Create a binding for a specific peer.</summary>
    public static ChannelBinding ForPeer(string peer, string agentId) =>
        new() { Peer = peer, AgentId = agentId };

    /// <summary>为指定公会创建绑定 / Create a binding for a specific guild.</summary>
    public static ChannelBinding ForGuild(string guild, string agentId) =>
        new() { Guild = guild, AgentId = agentId };

    /// <summary>为指定团队创建绑定 / Create a binding for a specific team.</summary>
    public static ChannelBinding ForTeam(string team, string agentId) =>
        new() { Team = team, AgentId = agentId };

    /// <summary>为指定账户创建绑定 / Create a binding for a specific account.</summary>
    public static ChannelBinding ForAccount(string account, string agentId) =>
        new() { Account = account, AgentId = agentId };

    /// <summary>为指定渠道创建绑定 / Create a binding for a specific channel.</summary>
    public static ChannelBinding ForChannel(string channel, string agentId) =>
        new() { Channel = channel, AgentId = agentId };
}

