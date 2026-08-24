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

using AgentScope.Core.Agent;

namespace AgentScope.Harness.Subagent;

/// <summary>
/// Subagent manager interface. Defines agent lifecycle operations.
/// 子 Agent 管理器接口。定义 Agent 生命周期操作。
/// </summary>
public interface ISubagentManager
{
    /// <summary>
    /// Gets an existing agent or creates a new one by spec reference.
    /// 获取已有 Agent 或根据 spec 引用创建新 Agent。
    /// </summary>
    /// <param name="specRef">The agent spec reference / Agent 规格引用</param>
    /// <returns>The agent instance / Agent 实例</returns>
    IAgent GetOrCreate(string specRef);

    /// <summary>
    /// Registers an agent by name.
    /// 按名称注册 Agent。
    /// </summary>
    /// <param name="name">The agent name / Agent 名称</param>
    /// <param name="agent">The agent instance / Agent 实例</param>
    void Register(string name, IAgent agent);

    /// <summary>
    /// Removes a registered agent by name.
    /// 按名称移除已注册的 Agent。
    /// </summary>
    /// <param name="name">The agent name / Agent 名称</param>
    void Remove(string name);
}
