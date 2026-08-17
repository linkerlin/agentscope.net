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
/// Subagent declaration. Loaded from Markdown + YAML front matter.
/// 子 Agent 声明。通过 Markdown + YAML front matter 加载。
/// </summary>
/// <param name="Name">Agent name / Agent 名称</param>
/// <param name="Description">Agent description / Agent 描述</param>
/// <param name="WorkspacePath">Optional workspace path / 可选工作区路径</param>
/// <param name="InlineBody">Optional inline body content / 可选内联正文</param>
/// <param name="RemoteUrl">Optional remote URL for remote agents / 可选远程 URL</param>
/// <param name="WorkspaceMode">Workspace isolation mode / 工作区隔离模式</param>
public sealed record SubagentDeclaration(
    string Name,
    string Description,
    string? WorkspacePath = null,
    string? InlineBody = null,
    string? RemoteUrl = null,
    WorkspaceMode WorkspaceMode = WorkspaceMode.Shared)
{
    /// <summary>Whether this is a remote agent / 是否为远程 Agent</summary>
    public bool IsRemote => RemoteUrl != null;
}

/// <summary>
/// Subagent workspace mode. Controls workspace isolation.
/// 子 Agent 工作区模式。控制工作区隔离策略。
/// </summary>
public enum WorkspaceMode
{
    /// <summary>Each subagent gets its own isolated workspace / 每个子 Agent 有独立工作区</summary>
    Isolated,
    /// <summary>All subagents share a common workspace / 所有子 Agent 共享工作区</summary>
    Shared
}

/// <summary>
/// Subagent factory delegate. Creates an agent from a declaration.
/// 子 Agent 工厂委托。根据声明创建 Agent。
/// </summary>
/// <param name="declaration">The subagent declaration / 子 Agent 声明</param>
/// <returns>The created agent instance / 创建的 Agent 实例</returns>
public delegate IAgent SubagentFactory(SubagentDeclaration declaration);
