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

using AgentScope.Harness.Gateway.Channel;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 网关引导器：组装网关 + 渠道集合，统一绑定与启停。
/// 对应 Java: io.agentscope.harness.agent.gateway.GatewayBootstrap
/// </summary>
public sealed class GatewayBootstrap : IAsyncDisposable
{
    private readonly IGateway _gateway;
    private readonly ChannelManager _channelManager;
    private readonly ChannelRuntimeContextResolver _resolver;
    private bool _started;

    /// <summary>
    /// 初始化网关引导器。
    /// Initialize the gateway bootstrap.
    /// </summary>
    /// <param name="gateway">网关实例 / The gateway instance.</param>
    /// <param name="channelManager">渠道管理器，可选 / Optional channel manager.</param>
    /// <exception cref="ArgumentNullException">gateway 为 null 时抛出 / Thrown when gateway is null.</exception>
    public GatewayBootstrap(IGateway gateway, ChannelManager? channelManager = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _channelManager = channelManager ?? new ChannelManager();
        _resolver = new ChannelRuntimeContextResolver();
    }

    /// <summary>
    /// 注册一个渠道到引导器。
    /// Register a channel with the bootstrap.
    /// </summary>
    /// <param name="channel">渠道实例 / The channel instance.</param>
    /// <returns>当前引导器实例（链式调用）/ This bootstrap instance for chaining.</returns>
    public GatewayBootstrap WithChannel(IChannel channel)
    {
        _channelManager.Register(channel);
        return this;
    }

    /// <summary>
    /// 绑定网关并启动所有已注册渠道。
    /// Bind the gateway and start all registered channels.
    /// </summary>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_started) return;
        _channelManager.Bind(_gateway);
        await _channelManager.StartAllAsync(ct);
        _started = true;
    }

    /// <summary>
    /// 停止所有已启动的渠道。
    /// Stop all started channels.
    /// </summary>
    /// <param name="ct">取消令牌 / Cancellation token.</param>
    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!_started) return;
        await _channelManager.StopAllAsync(ct);
        _started = false;
    }

    /// <summary>
    /// 获取渠道运行时上下文解析器。
    /// Gets the channel runtime context resolver.
    /// </summary>
    public ChannelRuntimeContextResolver Resolver => _resolver;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
