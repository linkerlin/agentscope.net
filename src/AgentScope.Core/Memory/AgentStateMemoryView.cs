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

using System.Collections.Generic;
using System.Linq;
using AgentScope.Core.Message;
using AgentScope.Core.State;

namespace AgentScope.Core.Memory;

/// <summary>
/// 以 AgentState.Context 为底层存储的内存视图：把 IMemory 操作直接映射到 AgentState，
/// 使 Agent 运行时上下文与可持久化状态保持一致。
/// 对应 Java: io.agentscope.core.memory.AgentStateMemoryView
/// </summary>
public class AgentStateMemoryView : IMemory
{
    private readonly AgentState _state;

    public AgentStateMemoryView(AgentState state)
    {
        _state = state ?? throw new System.ArgumentNullException(nameof(state));
    }

    /// <summary>底层 AgentState。</summary>
    public AgentState State => _state;

    /// <inheritdoc />
    public void Add(Msg message) => _state.Context.Add(message);

    /// <inheritdoc />
    public List<Msg> GetAll() => _state.Context.ToList();

    /// <inheritdoc />
    public List<Msg> GetRecent(int count) =>
        _state.Context.Skip(System.Math.Max(0, _state.Context.Count - count)).ToList();

    /// <inheritdoc />
    public void Clear() => _state.Context.Clear();

    /// <inheritdoc />
    public int Count() => _state.Context.Count;

    /// <inheritdoc />
    public bool Delete(string messageId)
    {
        var removed = _state.Context.RemoveAll(m => m.Id == messageId);
        return removed > 0;
    }
}
