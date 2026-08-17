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

    /// <summary>
    /// 注册一个渠道到管理器。
    /// Register a channel into the manager.
    /// </summary>
    /// <param name="channel">要注册的渠道实例 / The channel instance to register.</param>
    /// <returns>当前管理器实例（链式调用）/ This manager instance for chaining.</returns>
    /// <exception cref="ArgumentNullException">channel 为 null 时抛出 / Thrown when channel is null.</exception>
    public ChannelManager Register(IChannel channel)
    {
        if (channel == null) throw new ArgumentNullException(nameof(channel));
        _channels[channel.ChannelId] = channel;
        if (_gateway != null) channel.Init(_gateway);
        return this;
    }

    /// <summary>
    /// 绑定网关并初始化所有已注册渠道。
    /// Bind the gateway and initialize all registered channels.
    /// </summary>
    /// <param name="gateway">网关实例 / The gateway instance.</param>
    /// <exception cref="ArgumentNullException">gateway 为 null 时抛出 / Thrown when gateway is null.</exception>
    public void Bind(IGateway gateway)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        foreach (var ch in _channels.Values)
        {
            ch.Init(gateway);
        }
    }

    /// <summary>
    /// 按渠道 ID 获取已注册的渠道实例。
    /// Get a registered channel by its ID.
    /// </summary>
    /// <param name="channelId">渠道 ID / The channel ID.</param>
    /// <returns>渠道实例，未找到时返回 null / The channel instance, or null if not found.</returns>
    public IChannel? Get(string channelId) =>
        _channels.TryGetValue(channelId ?? "", out var ch) ? ch : null;

    /// <summary>
    /// 启动所有已注册的渠道。
    /// Start all registered channels.
    /// </summary>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    public async Task StartAllAsync(CancellationToken ct = default)
    {
        foreach (var ch in _channels.Values)
        {
            await ch.StartAsync(ct);
        }
    }

    /// <summary>
    /// 停止所有已注册的渠道。
    /// Stop all registered channels.
    /// </summary>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    public async Task StopAllAsync(CancellationToken ct = default)
    {
        foreach (var ch in _channels.Values)
        {
            await ch.StopAsync(ct);
        }
    }

    /// <summary>
    /// 获取当前所有已注册渠道的 ID 集合。
    /// Gets the IDs of all currently registered channels.
    /// </summary>
    public IReadOnlyCollection<string> ChannelIds => _channels.Keys.ToArray();
}
