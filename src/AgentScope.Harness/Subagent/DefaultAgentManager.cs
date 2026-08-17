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

using System.Collections.Concurrent;
using AgentScope.Core.Agent;

namespace AgentScope.Harness.Subagent;

/// <summary>
/// Default subagent manager. Manages agent lifecycle with thread-safe registry.
/// 默认子 Agent 管理器。提供线程安全的 Agent 注册与生命周期管理。
/// </summary>
public sealed class DefaultAgentManager : ISubagentManager
{
    private readonly ConcurrentDictionary<string, IAgent> _registry = new(StringComparer.OrdinalIgnoreCase);
    private readonly SubagentFactory? _factory;

    /// <summary>
    /// Initializes a new instance of the DefaultAgentManager.
    /// 初始化默认 Agent 管理器。
    /// </summary>
    /// <param name="factory">Optional factory for creating agents / 可选的 Agent 工厂</param>
    public DefaultAgentManager(SubagentFactory? factory = null)
    {
        _factory = factory;
    }

    /// <inheritdoc />
    public IAgent GetOrCreate(string specRef)
    {
        if (_registry.TryGetValue(specRef, out var existing))
            return existing;

        IAgent agent;
        if (_factory != null)
        {
            var decl = AgentSpecLoader.Load(specRef);
            agent = _factory(decl);
        }
        else
        {
            throw new InvalidOperationException($"未注册子 Agent: {specRef}");
        }

        _registry[specRef] = agent;
        return agent;
    }

    /// <inheritdoc />
    public void Register(string name, IAgent agent) => _registry[name] = agent;

    /// <inheritdoc />
    public void Remove(string name) => _registry.TryRemove(name, out _);
}
