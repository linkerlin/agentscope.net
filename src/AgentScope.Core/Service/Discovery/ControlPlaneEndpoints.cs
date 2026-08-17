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
/// 控制面 REST API 端点常量。对标 Java aistio control plane + admin starter。
/// </summary>
public static class ControlPlaneEndpoints
{
    public const string BasePath = "/api/v1/agents";
    public const string HealthPath = "/api/v1/health";
    public const string DashboardPath = "/api/v1/dashboard";

    // Agent 管理
    public const string Register = $"{BasePath}/register";
    public const string Unregister = $"{BasePath}/{{agentId}}/unregister";
    public const string List = BasePath;
    public const string GetById = $"{BasePath}/{{agentId}}";
    public const string Heartbeat = $"{BasePath}/{{agentId}}/heartbeat";

    // Dashboard 统计
    public const string Stats = $"{DashboardPath}/stats";
    public const string AgentsByRuntime = $"{DashboardPath}/agents-by-runtime";
}

/// <summary>
/// Dashboard 统计数据。对标 aistio DashboardStats。
/// </summary>
public sealed record DashboardStats
{
    public int TotalAgents { get; init; }
    public int ActiveAgents { get; init; }
    public int StaleAgents { get; init; }
    public IReadOnlyList<AgentGroup> ByRuntime { get; init; } = [];
    public IReadOnlyList<AgentGroup> ByContractLevel { get; init; } = [];
}

public sealed record AgentGroup(string Key, int Count);
