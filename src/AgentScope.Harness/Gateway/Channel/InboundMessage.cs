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

