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
/// 工具集状态，用于持久化当前激活的工具组等。
/// </summary>
public record ToolkitState(IReadOnlySet<string> ActiveGroups) : IState
{
    public static ToolkitState Empty => new ToolkitState(new HashSet<string>());
}
