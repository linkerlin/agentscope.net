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
/// DataPlane 自注册心跳服务。对标 aistio DataPlaneSelfRegistration。
/// 通过 IHostedService 周期性向控制面发送心跳并更新本地注册表。
/// </summary>
public sealed class DataPlaneSelfRegistration(
    DataPlaneRegistry registry,
    IDataPlaneControlPlaneClient controlPlane,
    TimeProvider clock) : IDisposable
{
    private Timer? _timer;

    public void Start()
    {
        _timer = new Timer(_ => _ = HeartbeatAsync(), null, clock.GetUtcNow().ToLocalTime().TimeOfDay, TimeSpan.FromSeconds(15));
    }

    public void Stop()
    {
        _timer?.Dispose();
        _timer = null;
    }

    private async Task HeartbeatAsync()
    {
        var caps = new Capabilities("agentscope-dotnet", 3, "1.2.0");
        await controlPlane.HeartbeatAsync(caps, CancellationToken.None);
        registry.Upsert(caps.Endpoint,
            new AgentSummary(caps.Runtime, caps.Runtime, caps.Endpoint, caps.ContractLevel.ToString()));
    }

    public void Dispose() => Stop();
}
