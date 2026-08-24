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
using AgentScope.Harness.Subagent;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// 子 Agent 实例化器：把 SubagentDeclaration 经工厂实例化为运行中的 IAgent，并缓存避免重复创建。
/// 对应 Java: io.agentscope.harness.agent.gateway.SubagentMaterializer
/// </summary>
public sealed class SubagentMaterializer
{
    private readonly SubagentFactory _factory;
    private readonly ConcurrentDictionary<string, IAgent> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 初始化子 Agent 实例化器。
    /// Initialize the subagent materializer.
    /// </summary>
    /// <param name="factory">子 Agent 工厂委托 / The subagent factory delegate.</param>
    /// <exception cref="ArgumentNullException">factory 为 null 时抛出 / Thrown when factory is null.</exception>
    public SubagentMaterializer(SubagentFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>
    /// 按声明实例化子 Agent（同名声明只创建一次，后续返回缓存实例）。
    /// Materialize a subagent from a declaration (same name creates only once, returns cached instance).
    /// </summary>
    /// <param name="declaration">子 Agent 声明 / The subagent declaration.</param>
    /// <param name="parentRc">父运行时上下文，可选 / Optional parent runtime context.</param>
    /// <returns>实例化的 Agent / The materialized agent.</returns>
    /// <exception cref="ArgumentNullException">declaration 为 null 时抛出 / Thrown when declaration is null.</exception>
    public IAgent Materialize(SubagentDeclaration declaration, RuntimeContext? parentRc = null)
    {
        if (declaration == null) throw new ArgumentNullException(nameof(declaration));
        return _cache.GetOrAdd(declaration.Name, _ =>
        {
            RuntimeContext.Current = parentRc;
            return _factory(declaration);
        });
    }

    /// <summary>
    /// 批量实例化多个子 Agent 声明。
    /// Materialize multiple subagent declarations at once.
    /// </summary>
    /// <param name="declarations">子 Agent 声明集合 / The declarations to materialize.</param>
    /// <param name="parentRc">父运行时上下文，可选 / Optional parent runtime context.</param>
    /// <returns>名称到 Agent 实例的映射 / A mapping of names to agent instances.</returns>
    public IReadOnlyDictionary<string, IAgent> MaterializeAll(
        IEnumerable<SubagentDeclaration> declarations, RuntimeContext? parentRc = null)
    {
        var result = new Dictionary<string, IAgent>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in declarations)
        {
            result[d.Name] = Materialize(d, parentRc);
        }

        return result;
    }

    /// <summary>
    /// 清理所有缓存的子 Agent 实例。
    /// Clear all cached subagent instances.
    /// </summary>
    public void Clear() => _cache.Clear();
}
