// Copyright 2024-2026 the original author or authors.
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

namespace AgentScope.Harness.Gateway.Channel;

/// <summary>
/// 渠道工厂：按渠道类型/配置创建 IChannel 实例。
/// 对应 Java: io.agentscope.harness.agent.gateway.channel.ChannelFactory
/// </summary>
public sealed class ChannelFactory
{
    private readonly Dictionary<string, Func<ChannelConfig, IChannel>> _builders =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>注册某渠道类型的构造器。</summary>
    public ChannelFactory Register(string channelType, Func<ChannelConfig, IChannel> builder)
    {
        _builders[channelType] = builder ?? throw new ArgumentNullException(nameof(builder));
        return this;
    }

    /// <summary>按类型创建渠道。</summary>
    public IChannel Create(string channelType, ChannelConfig config)
    {
        if (_builders.TryGetValue(channelType, out var builder))
        {
            return builder(config);
        }

        throw new InvalidOperationException($"未注册的渠道类型: {channelType}");
    }

    /// <summary>是否已注册某渠道类型。</summary>
    public bool CanCreate(string channelType) => _builders.ContainsKey(channelType);
}
