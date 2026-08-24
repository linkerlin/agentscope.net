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

using AgentScope.Core.Message;
namespace AgentScope.Harness.Gateway.Channel;

/// <summary>消息渠道接口，对�?Java Channel</summary>
public interface IChannel
{
    string ChannelId { get; }
    ChannelConfig Config { get; }

    void Init(IGateway gateway);
    Task StartAsync(CancellationToken ct = default);
    Task StopAsync(CancellationToken ct = default);

    Task<Msg> DispatchAsync(InboundMessage message, CancellationToken ct = default);
    void Deliver(OutboundAddress address, IReadOnlyList<Msg> messages);
}

