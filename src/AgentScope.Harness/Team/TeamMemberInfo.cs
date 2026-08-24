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
/// Read-only information about a team member.
/// 团队成员的只读信息。
/// </summary>
/// <param name="AgentId">Agent ID / 代理 ID</param>
/// <param name="Name">Display name / 显示名称</param>
/// <param name="Role">Role in the team / 团队角色</param>
/// <param name="JoinedAt">Join timestamp / 加入时间戳</param>
public sealed record TeamMemberInfo(string AgentId, string Name, string Role, DateTime JoinedAt);
