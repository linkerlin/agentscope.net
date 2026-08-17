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
/// Represents the full context of a team, including members and metadata.
/// 表示团队的完整上下文，包括成员和元数据。
/// </summary>
/// <param name="TeamId">Unique team ID / 唯一团队 ID</param>
/// <param name="Name">Team name / 团队名称</param>
/// <param name="MemberIds">List of member IDs / 成员 ID 列表</param>
/// <param name="Metadata">Additional key-value metadata / 附加键值元数据</param>
public sealed record TeamContext(string TeamId, string Name, List<string> MemberIds, Dictionary<string, string> Metadata);
