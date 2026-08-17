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

namespace AgentScope.Harness.Workspace;

/// <summary>
/// 工作区路径常量。对标 Java WorkspaceConstants。
/// </summary>
public static class WorkspaceConstants
{
    /// <summary>默认工作区根目录。对标 Java <c>DEFAULT_WORKSPACE_ROOT</c>。</summary>
    public const string DefaultWorkspaceRoot = ".agentscope/workspace";

    public const string AgentsMd = "AGENTS.md";
    public const string MemoryMd = "MEMORY.md";
    public const string KnowledgeMd = "KNOWLEDGE.md";
    public const string ToolsJson = "tools.json";

    public const string MemoryDir = "memory";
    public const string WorkspaceDir = "workspace";
    public const string SkillsDir = "skills";
    public const string SubagentsDir = "subagents";
    public const string KnowledgeDir = "knowledge";
    public const string RulesDir = "rules";
    public const string AgentsDir = "agents";
    public const string SessionsDir = "sessions";
    public const string TasksDir = "tasks";
    public const string IndexDir = ".index";

    /// <summary>归档目录（记忆维护把过期日文件移到这里）。</summary>
    public const string MemoryArchiveDir = "memory/archive";

    public const string SessionsStore = "sessions.json";
    public const string SessionContextExt = ".jsonl";
    public const string SessionLogExt = ".log.jsonl";
}
