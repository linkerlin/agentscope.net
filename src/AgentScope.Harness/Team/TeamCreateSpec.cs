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

namespace AgentScope.Harness.Team;

/// <summary>
/// Specifies parameters for creating a new team.
/// 创建新团队的参数规格。
/// </summary>
/// <param name="Name">Team name / 团队名称</param>
/// <param name="Description">Optional team description / 可选的团队描述</param>
/// <param name="MemberIds">Optional initial member IDs / 可选的初始成员 ID 列表</param>
public sealed record TeamCreateSpec(string Name, string? Description = null, List<string>? MemberIds = null);
