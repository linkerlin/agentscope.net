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

    public GatewayBootstrap(IGateway gateway, ChannelManager? channelManager = null)
    {
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _channelManager = channelManager ?? new ChannelManager();
        _resolver = new ChannelRuntimeContextResolver();
    }

    /// <summary>注册渠道。</summary>
    public GatewayBootstrap WithChannel(IChannel channel)
    {
        _channelManager.Register(channel);
        return this;
    }

    /// <summary>绑定并启动所有渠道。</summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (_started) return;
        _channelManager.Bind(_gateway);
        await _channelManager.StartAllAsync(ct);
        _started = true;
    }

    /// <summary>停止所有渠道。</summary>
    public async Task StopAsync(CancellationToken ct = default)
    {
        if (!_started) return;
        await _channelManager.StopAllAsync(ct);
        _started = false;
    }

    /// <summary>暴露渠道运行时上下文解析器。</summary>
    public ChannelRuntimeContextResolver Resolver => _resolver;

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}
