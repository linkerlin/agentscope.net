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

namespace AgentScope.Extensions.Skill;

/// <summary>
/// 技能仓库接口。对标 Java AgentSkillRepository。
/// </summary>
public interface ISkillRepository
{
    Task<Skill?> GetSkillAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetAllSkillNamesAsync(CancellationToken ct = default);
    Task<bool> SkillExistsAsync(string name, CancellationToken ct = default);
}

public sealed record Skill(string Name, string Description, string Content, string? Source = null);
