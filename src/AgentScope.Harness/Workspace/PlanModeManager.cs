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

/// <summary>PLAN/BUILD 模式管理器，对应 Java PlanModeManager</summary>
public sealed class PlanModeManager
{
    public PlanMode CurrentMode { get; private set; } = PlanMode.Build;

    public event Action<PlanMode>? OnModeChanged;

    public void SetMode(PlanMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        OnModeChanged?.Invoke(mode);
    }

    public void Toggle()
    {
        SetMode(CurrentMode == PlanMode.Plan ? PlanMode.Build : PlanMode.Plan);
    }
}

public enum PlanMode
{
    Plan,
    Build
}
