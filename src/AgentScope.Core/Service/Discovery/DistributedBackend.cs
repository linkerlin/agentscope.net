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
/// One-click distributed backend configuration. Corresponds to Java DistributedBackend.
/// Encapsulates all components required for distributed deployment: AgentRegistry, DataPlaneRegistry, and discovery mechanism.
/// 一键分布式后端配置。对标 Java DistributedBackend。
/// 封装分布式部署所需的全部组件：AgentRegistry、DataPlaneRegistry、发现机制。
/// </summary>
public sealed class DistributedBackend
{
    /// <summary>
    /// The service name for this distributed backend instance.
    /// 此分布式后端实例的服务名称。
    /// </summary>
    private readonly string _serviceName;

    /// <summary>
    /// The endpoint address for this distributed backend instance.
    /// 此分布式后端实例的端点地址。
    /// </summary>
    private readonly string _endpoint;

    /// <summary>
    /// The registry type (e.g., "memory", "nacos").
    /// 注册表类型（例如 "memory"、"nacos"）。
    /// </summary>
    private readonly string? _registryType;

    /// <summary>
    /// Gets the agent registry for service discovery.
    /// 获取用于服务发现的 Agent 注册表。
    /// </summary>
    public IAgentRegistry? AgentRegistry { get; private set; }

    /// <summary>
    /// Gets the data plane registry for heartbeat and stale detection.
    /// 获取用于心跳和失效检测的数据面注册表。
    /// </summary>
    public DataPlaneRegistry? DataPlaneRegistry { get; private set; }

    /// <summary>
    /// Gets the control plane service for registration management.
    /// 获取用于注册管理的控制面服务。
    /// </summary>
    public ControlPlaneService? ControlPlane { get; private set; }

    /// <summary>
    /// Private constructor to enforce creation via static factory methods.
    /// 私有构造函数，强制通过静态工厂方法创建。
    /// </summary>
    private DistributedBackend(string serviceName, string endpoint, string? registryType)
    {
        _serviceName = serviceName;
        _endpoint = endpoint;
        _registryType = registryType;
    }

    /// <summary>
    /// Creates a distributed backend with in-memory registry, data plane, and control plane.
    /// 一步创建分布式后端：内存注册 + 数据面 + 控制面。
    /// </summary>
    public static DistributedBackend CreateInMemory(string serviceName, string endpoint)
    {
        var backend = new DistributedBackend(serviceName, endpoint, "memory");
        backend.AgentRegistry = new InMemoryAgentRegistry();
        backend.DataPlaneRegistry = new DataPlaneRegistry();
        backend.ControlPlane = new ControlPlaneService(backend.DataPlaneRegistry, backend.AgentRegistry);
        return backend;
    }

    /// <summary>
    /// Creates a distributed backend with Nacos registry center.
    /// 使用 Nacos 注册中心创建分布式后端。
    /// </summary>
    public static DistributedBackend CreateWithNacos(string serviceName, string endpoint, string nacosAddr)
    {
        var backend = new DistributedBackend(serviceName, endpoint, "nacos");
        backend.DataPlaneRegistry = new DataPlaneRegistry();
        // NacosAgentRegistry requires external HttpClient registration
        // NacosAgentRegistry 需要在外部注册 HttpClient
        return backend;
    }

    /// <summary>
    /// Registers the current service instance with the registry center.
    /// 注册当前服务实例到注册中心。
    /// </summary>
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

    /// <summary>
    /// Sends a heartbeat to keep the agent alive in the registry.
    /// 发送心跳以保持 Agent 在注册表中的活跃状态。
    /// </summary>
    public void Heartbeat(string agentId) => DataPlaneRegistry?.Heartbeat(agentId);
}

/// <summary>
/// Configuration options for the distributed backend.
/// 分布式后端配置选项。
/// </summary>
public sealed record DistributedBackendOptions
{
    /// <summary>
    /// The service name for the agent. Default is "agentscope-agent".
    /// Agent 的服务名称。默认为 "agentscope-agent"。
    /// </summary>
    public string ServiceName { get; init; } = "agentscope-agent";

    /// <summary>
    /// The endpoint address for the agent. Default is "http://localhost:5000".
    /// Agent 的端点地址。默认为 "http://localhost:5000"。
    /// </summary>
    public string Endpoint { get; init; } = "http://localhost:5000";

    /// <summary>
    /// The registry type. Supported values: "memory", "nacos". Default is "memory".
    /// 注册表类型。支持的值："memory"、"nacos"。默认为 "memory"。
    /// </summary>
    public string RegistryType { get; init; } = "memory";

    /// <summary>
    /// The Nacos server address (required when RegistryType is "nacos").
    /// Nacos 服务器地址（当 RegistryType 为 "nacos" 时需要）。
    /// </summary>
    public string? NacosAddr { get; init; }

    /// <summary>
    /// The heartbeat interval. Default is 15 seconds.
    /// 心跳间隔。默认为 15 秒。
    /// </summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(15);
}
