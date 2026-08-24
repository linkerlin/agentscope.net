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

namespace AgentScope.Core.State;

/// <summary>
/// 计划模式上下文状态：记录当前是否处于 Plan 模式、待批准的计划草案与已批准计划。
/// 对应 Java: io.agentscope.core.state.PlanModeContextState
/// </summary>
public class PlanModeContextState : IState
{
    /// <summary>是否处于计划模式（构建模式）</summary>
    public bool InPlanMode { get; set; }

    /// <summary>待用户批准的计划草稿（Markdown 文本）</summary>
    public string? PendingPlanDraft { get; set; }

    /// <summary>已批准的计划文本</summary>
    public string? ApprovedPlan { get; set; }

    /// <summary>计划版本，每次修订递增</summary>
    public int PlanVersion { get; set; }

    public PlanModeContextState() { }

    public PlanModeContextState(bool inPlanMode)
    {
        InPlanMode = inPlanMode;
    }

    /// <summary>进入计划模式</summary>
    public void Enter() => InPlanMode = true;

    /// <summary>退出计划模式</summary>
    public void Exit() => InPlanMode = false;

    /// <summary>提交待批准草稿</summary>
    public void SubmitDraft(string draft)
    {
        PendingPlanDraft = draft;
        PlanVersion++;
    }

    /// <summary>批准当前草稿</summary>
    public void Approve()
    {
        ApprovedPlan = PendingPlanDraft;
        PendingPlanDraft = null;
    }
}
