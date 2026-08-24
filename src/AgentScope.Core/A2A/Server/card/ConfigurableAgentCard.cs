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

using AgentScope.Core.Service.Discovery;

namespace AgentScope.Core.A2A.Server.Card;

/// <summary>
/// 可配置的 AgentCard Builder。对标 Java ConfigurableAgentCard。
/// </summary>
public sealed class ConfigurableAgentCard
{
    public string Name { get; set; } = "a2a-agent";
    public string Description { get; set; } = "A2A Agent";
    public string? Url { get; set; }
    public string? Provider { get; set; }
    public List<string> Skills { get; set; } = [];
    public bool Streaming { get; set; } = true;

    public AgentCard Build() => new(
        Guid.NewGuid().ToString(),
        Name, Description,
        Url ?? $"http://localhost:5000",
        Provider,
        Skills);
}
