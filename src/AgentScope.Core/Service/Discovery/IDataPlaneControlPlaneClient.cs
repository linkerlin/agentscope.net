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
/// Data plane to control plane communication interface. Corresponds to aistio DataPlaneClient.
/// 数据面→控制面通信接口。对标 aistio DataPlaneClient。
/// </summary>
public interface IDataPlaneControlPlaneClient
{
    /// <summary>
    /// Sends a heartbeat with capability declaration to the control plane.
    /// 向控制面发送心跳及能力声明。
    /// </summary>
    ValueTask HeartbeatAsync(Capabilities caps, CancellationToken ct = default);

    /// <summary>
    /// Pulls data plane configuration from the control plane for a specific agent.
    /// 从控制面拉取指定 Agent 的数据面配置。
    /// </summary>
    ValueTask<DataPlaneConfig> PullConfigAsync(string agentId, CancellationToken ct = default);
}

/// <summary>
/// Capability declaration for an agent runtime. Corresponds to aistio Capabilities.
/// Agent 运行时的能力声明。对标 aistio Capabilities。
/// </summary>
public readonly record struct Capabilities(string Runtime, int ContractLevel, string Version)
{
    /// <summary>
    /// Network endpoint where the agent can be reached.
    /// Agent 可访问的网络端点。
    /// </summary>
    public string Endpoint { get; init; } = "";
}

/// <summary>
/// Data plane configuration pulled from the control plane.
/// 从控制面拉取的数据面配置。
/// </summary>
public readonly record struct DataPlaneConfig(
    IReadOnlyList<string> AllowedTools,
    IReadOnlyDictionary<string, string> Variables);
