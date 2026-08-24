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

/// <summary>渠道路由配置，对�?Java ChannelConfig</summary>
public sealed record ChannelConfig
{
    public string ChannelId { get; init; }
    public string DefaultAgentId { get; init; } = "";
    public DmScope DmScope { get; init; } = DmScope.Main;
    public IReadOnlyList<ChannelBinding> Bindings { get; init; } = [];

    public static ChannelConfig Of(string channelId) => new() { ChannelId = channelId };

    public static ChannelConfig Of(string channelId, string defaultAgentId) =>
        new() { ChannelId = channelId, DefaultAgentId = defaultAgentId };
}

