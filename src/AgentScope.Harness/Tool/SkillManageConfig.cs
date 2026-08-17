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

namespace AgentScope.Harness.Tool;

/// <summary>
/// 技能管理配置：控制 SkillManageTool 的行为（提议/审批/提升阈值等）。
/// 对应 Java: io.agentscope.harness.agent.tool.SkillManageConfig
/// </summary>
public sealed class SkillManageConfig
{
    /// <summary>是否允许 Agent 动态提议新技能。</summary>
    public bool AllowPropose { get; set; } = true;

    /// <summary>是否需要人工审批才能激活新技能。</summary>
    public bool RequireApproval { get; set; } = true;

    /// <summary>提升为正式技能所需的最小使用次数。</summary>
    public int MinUsageToPromote { get; set; } = 5;

    /// <summary>技能草稿目录（相对工作区）。</summary>
    public string DraftsDir { get; set; } = ".agentscope/skills/_drafts";

    /// <summary>提议技能时是否做安全扫描。</summary>
    public bool SecurityScanOnPropose { get; set; } = true;

    public static SkillManageConfig Default => new();
}
