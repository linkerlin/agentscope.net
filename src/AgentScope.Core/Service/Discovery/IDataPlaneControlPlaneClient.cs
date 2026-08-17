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
/// 数据面→控制面通信接口。对标 aistio DataPlaneClient。
/// </summary>
public interface IDataPlaneControlPlaneClient
{
    ValueTask HeartbeatAsync(Capabilities caps, CancellationToken ct = default);
    ValueTask<DataPlaneConfig> PullConfigAsync(string agentId, CancellationToken ct = default);
}

/// <summary>
/// 能力声明。对标 aistio Capabilities。
/// </summary>
public readonly record struct Capabilities(string Runtime, int ContractLevel, string Version)
{
    public string Endpoint { get; init; } = "";
}

/// <summary>
/// 数据面配置（从控制面拉取）。
/// </summary>
public readonly record struct DataPlaneConfig(
    IReadOnlyList<string> AllowedTools,
    IReadOnlyDictionary<string, string> Variables);
