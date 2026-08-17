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

namespace AgentScope.Core.Service.Discovery;

/// <summary>
/// 一键分布式后端配置。对标 Java DistributedBackend。
/// 封装分布式部署所需的全部组件：AgentRegistry、DataPlaneRegistry、发现机制。
/// </summary>
public sealed class DistributedBackend
{
    private readonly string _serviceName;
    private readonly string _endpoint;
    private readonly string? _registryType;

    public IAgentRegistry? AgentRegistry { get; private set; }
    public DataPlaneRegistry? DataPlaneRegistry { get; private set; }
    public ControlPlaneService? ControlPlane { get; private set; }

    private DistributedBackend(string serviceName, string endpoint, string? registryType)
    {
        _serviceName = serviceName;
        _endpoint = endpoint;
        _registryType = registryType;
    }

    /// <summary>一步创建分布式后端：内存注册 + 数据面 + 控制面</summary>
    public static DistributedBackend CreateInMemory(string serviceName, string endpoint)
    {
        var backend = new DistributedBackend(serviceName, endpoint, "memory");
        backend.AgentRegistry = new InMemoryAgentRegistry();
        backend.DataPlaneRegistry = new DataPlaneRegistry();
        backend.ControlPlane = new ControlPlaneService(backend.DataPlaneRegistry, backend.AgentRegistry);
        return backend;
    }

    /// <summary>使用 Nacos 注册中心创建分布式后端</summary>
    public static DistributedBackend CreateWithNacos(string serviceName, string endpoint, string nacosAddr)
    {
        var backend = new DistributedBackend(serviceName, endpoint, "nacos");
        backend.DataPlaneRegistry = new DataPlaneRegistry();
        // NacosAgentRegistry 需要在外部注册 HttpClient
        return backend;
    }

    /// <summary>注册当前服务实例到注册中心</summary>
    public async Task RegisterAsync(CancellationToken ct = default)
    {
        var card = new AgentCard(
            Guid.NewGuid().ToString(),
            _serviceName,
            $"Distributed Agent: {_serviceName}",
            _endpoint);

        if (ControlPlane != null)
            await ControlPlane.RegisterAsync(card, ct);
        else if (AgentRegistry != null)
            await AgentRegistry.RegisterAsync(card, ct);
    }

    /// <summary>心跳保活</summary>
    public void Heartbeat(string agentId) => DataPlaneRegistry?.Heartbeat(agentId);
}

/// <summary>
/// 分布式后端配置选项。
/// </summary>
public sealed record DistributedBackendOptions
{
    public string ServiceName { get; init; } = "agentscope-agent";
    public string Endpoint { get; init; } = "http://localhost:5000";
    public string RegistryType { get; init; } = "memory";
    public string? NacosAddr { get; init; }
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(15);
}
