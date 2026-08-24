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

namespace AgentScope.Harness;

/// <summary>
/// Sandbox/state isolation scope. Counterpart to Java IsolationScope.
/// 沙箱/状态隔离作用域。对标 Java IsolationScope。
/// Defines the granularity at which agent state and sandbox resources are isolated.
/// 定义 Agent 状态和沙箱资源的隔离粒度。
/// </summary>
public enum IsolationScope
{
    /// <summary>
    /// Isolate per session. Each conversation session has its own state.
    /// 按会话隔离。每个对话会话拥有独立状态。
    /// </summary>
    Session,

    /// <summary>
    /// Isolate per user. All sessions for the same user share state.
    /// 按用户隔离。同一用户的所有会话共享状态。
    /// </summary>
    User,

    /// <summary>
    /// Isolate per agent instance.
    /// 按 Agent 实例隔离。
    /// </summary>
    Agent,

    /// <summary>
    /// Global scope. State is shared across all agents, users, and sessions.
    /// 全局作用域。状态在所有 Agent、用户和会话间共享。
    /// </summary>
    Global
}
