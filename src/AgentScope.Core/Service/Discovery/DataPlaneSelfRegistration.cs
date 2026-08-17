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
/// DataPlane self-registration heartbeat service. Corresponds to aistio DataPlaneSelfRegistration.
/// Periodically sends heartbeats to the control plane via IHostedService and updates the local registry.
/// DataPlane 自注册心跳服务。对标 aistio DataPlaneSelfRegistration。
/// 通过 IHostedService 周期性向控制面发送心跳并更新本地注册表。
/// </summary>
public sealed class DataPlaneSelfRegistration(
    DataPlaneRegistry registry,
    IDataPlaneControlPlaneClient controlPlane,
    TimeProvider clock) : IDisposable
{
    /// <summary>
    /// Internal timer for periodic heartbeat execution.
    /// 用于周期性执行心跳的内部定时器。
    /// </summary>
    private Timer? _timer;

    /// <summary>
    /// Starts the periodic heartbeat timer. The first heartbeat is scheduled immediately,
    /// and subsequent heartbeats occur every 15 seconds.
    /// 启动周期性心跳定时器。首次心跳立即执行，后续每 15 秒执行一次。
    /// </summary>
    public void Start()
    {
        _timer = new Timer(_ => _ = HeartbeatAsync(), null, clock.GetUtcNow().ToLocalTime().TimeOfDay, TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Stops the periodic heartbeat timer and releases resources.
    /// 停止周期性心跳定时器并释放资源。
    /// </summary>
    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    /// <summary>
    /// Executes a single heartbeat cycle: sends capabilities to the control plane
    /// and updates the local data plane registry with the current agent summary.
    /// 执行单次心跳周期：向控制面发送能力信息并更新本地数据面注册表中的 Agent 摘要。
    /// </summary>
    private async Task HeartbeatAsync()
    {
        var caps = new Capabilities("agentscope-dotnet", 3, "1.2.0");
        await controlPlane.HeartbeatAsync(caps, CancellationToken.None);
        registry.Upsert(caps.Endpoint,
            new AgentSummary(caps.Runtime, caps.Runtime, caps.Endpoint, caps.ContractLevel.ToString()));
    }

    /// <summary>
    /// Disposes the timer when the service is no longer needed.
    /// 在服务不再需要时释放定时器资源。
    /// </summary>
    public void Dispose() => Stop();
}
