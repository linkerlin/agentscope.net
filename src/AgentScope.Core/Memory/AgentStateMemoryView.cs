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

using System.Collections.Generic;
using System.Linq;
using AgentScope.Core.Message;
using AgentScope.Core.State;

namespace AgentScope.Core.Memory;

/// <summary>
/// Memory view backed by AgentState.Context: maps IMemory operations directly
/// to AgentState, keeping the agent runtime context consistent with the
/// persistable state.
///
/// 以 AgentState.Context 为底层存储的内存视图：把 IMemory 操作直接映射到 AgentState，
/// 使 Agent 运行时上下文与可持久化状态保持一致。
/// Corresponds to Java: io.agentscope.core.memory.AgentStateMemoryView
/// </summary>
public class AgentStateMemoryView : IMemory
{
    /// <summary>
    /// The underlying agent state.
    /// 底层 Agent 状态。
    /// </summary>
    private readonly AgentState _state;

    /// <summary>
    /// Initializes a new instance of AgentStateMemoryView.
    /// 初始化 AgentStateMemoryView 的新实例。
    /// </summary>
    /// <param name="state">The agent state to wrap. / 要包装的 Agent 状态。</param>
    public AgentStateMemoryView(AgentState state)
    {
        _state = state ?? throw new System.ArgumentNullException(nameof(state));
    }

    /// <summary>
    /// The underlying AgentState.
    /// 底层 AgentState。
    /// </summary>
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
