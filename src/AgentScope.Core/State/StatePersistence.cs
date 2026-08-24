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

namespace AgentScope.Core.State;

/// <summary>
/// 状态持久化配置，指定哪些模块由 Session 管理持久化。
/// </summary>
public record StatePersistence(
    bool MemoryManaged = true,
    bool ToolkitManaged = true,
    bool PlanNotebookManaged = true)
{
    /// <summary>全部由 Session 管理</summary>
    public static StatePersistence All => new(true, true, true);

    /// <summary>不持久化任何模块</summary>
    public static StatePersistence None => new(false, false, false);
}
