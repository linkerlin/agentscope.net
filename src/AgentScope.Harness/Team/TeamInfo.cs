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
/// Read-only information about a team.
/// 团队的只读信息。
/// </summary>
/// <param name="Id">Unique team ID / 唯一团队 ID</param>
/// <param name="Name">Team name / 团队名称</param>
/// <param name="Description">Team description / 团队描述</param>
/// <param name="MemberCount">Number of members / 成员数量</param>
/// <param name="CreatedAt">Team creation timestamp / 团队创建时间戳</param>
public sealed record TeamInfo(string Id, string Name, string Description, int MemberCount, DateTime CreatedAt);
