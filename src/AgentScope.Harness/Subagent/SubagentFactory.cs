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
/// Subagent factory interface. Creates subagent instances from parent runtime context.
/// 子代理工厂接口。根据父运行时上下文创建子 Agent 实例。
/// </summary>
public interface ISubagentFactory
{
    /// <summary>
    /// Creates a subagent from the parent runtime context.
    /// 根据父运行时上下文创建子 Agent。
    /// </summary>
    /// <param name="parentRc">The parent runtime context / 父运行时上下文</param>
    /// <returns>The created agent / 创建的 Agent</returns>
    IAgent Create(RuntimeContext parentRc);
}
