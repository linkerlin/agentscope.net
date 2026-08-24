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

/// <summary>消息对等体，对应 Java Peer</summary>
public sealed record Peer(PeerKind Kind, string Id)
{
    public string Key => $"{Kind.ToString().ToLowerInvariant()}:{Id}";

    public static Peer Direct(string id) => new(PeerKind.Direct, id);
    public static Peer Channel(string id) => new(PeerKind.Channel, id);
    public static Peer Group(string id) => new(PeerKind.Group, id);
    public static Peer Thread(string id) => new(PeerKind.Thread, id);
}

