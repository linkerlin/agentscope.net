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

/// <summary>出站地址，对�?Java OutboundAddress</summary>
public sealed record OutboundAddress
{
    public string ChannelId { get; init; } = "";
    public string? AccountId { get; init; }
    public string To { get; init; } = "";
    public string? ThreadId { get; init; }

    public static OutboundAddress Direct(string channelId, string to) =>
        new() { ChannelId = channelId, To = to };
}

