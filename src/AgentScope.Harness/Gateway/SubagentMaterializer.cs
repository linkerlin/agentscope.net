// Copyright 2024-2026 the original author or authors.
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

    public SubagentMaterializer(SubagentFactory factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    /// <summary>按声明实例化子 Agent（同名声明只创建一次）。</summary>
    public IAgent Materialize(SubagentDeclaration declaration, RuntimeContext? parentRc = null)
    {
        if (declaration == null) throw new ArgumentNullException(nameof(declaration));
        return _cache.GetOrAdd(declaration.Name, _ =>
        {
            RuntimeContext.Current = parentRc;
            return _factory(declaration);
        });
    }

    /// <summary>批量实例化。</summary>
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

    /// <summary>清理缓存的实例。</summary>
    public void Clear() => _cache.Clear();
}
