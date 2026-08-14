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

using System.Collections.Concurrent;
using AgentScope.Harness.Gateway.Channel;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 渠道管理器：维护一组已注册渠道，统一初始化、启停与按ID分发消息。
/// 对应 Java: io.agentscope.harness.agent.gateway.ChannelManager
/// </summary>
public sealed class ChannelManager
{
    private readonly ConcurrentDictionary<string, IChannel> _channels = new(StringComparer.OrdinalIgnoreCase);
    private IGateway? _gateway;

    /// <summary>注册渠道。</summary>
    public ChannelManager Register(IChannel channel)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        _channels[channel.ChannelId] = channel;
        if (_gateway != null) channel.Init(_gateway);
        return this;
    }

    /// <summary>绑定网关并初始化所有渠道。</summary>
    public void Bind(IGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        foreach (var ch in _channels.Values)
        {
            ch.Init(gateway);
        }
    }

    /// <summary>按ID获取渠道。</summary>
    public IChannel? Get(string channelId) =>
        _channels.TryGetValue(channelId ?? "", out var ch) ? ch : null;

    /// <summary>启动所有渠道。</summary>
    public async Task StartAllAsync(CancellationToken ct = default)
    {
        foreach (var ch in _channels.Values)
        {
            await ch.StartAsync(ct);
        }
    }

    /// <summary>停止所有渠道。</summary>
    public async Task StopAllAsync(CancellationToken ct = default)
    {
        foreach (var ch in _channels.Values)
        {
            await ch.StopAsync(ct);
        }
    }

    /// <summary>当前已注册渠道ID。</summary>
    public IReadOnlyCollection<string> ChannelIds => _channels.Keys.ToArray();
}
