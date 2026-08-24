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

namespace AgentScope.Harness.Skill.Runtime;

/// <summary>不可变的 skill 快照，按 skillId 索引，对应 Java SkillCatalog</summary>
public sealed class SkillCatalog
{
    private readonly Dictionary<string, HarnessSkillEntry> _entries;

    private SkillCatalog(Dictionary<string, HarnessSkillEntry> entries)
    {
        _entries = entries;
    }

    public static SkillCatalog Empty => new(new());

    public static SkillCatalog Of(IEnumerable<HarnessSkillEntry> entries)
    {
        var dict = entries.ToDictionary(e => e.SkillId, e => e);
        return new SkillCatalog(dict);
    }

    public HarnessSkillEntry? Get(string skillId) =>
        _entries.TryGetValue(skillId, out var e) ? e : null;

    public IReadOnlyCollection<HarnessSkillEntry> All => _entries.Values;

    public IReadOnlyCollection<string> Ids => _entries.Keys;

    public bool IsEmpty => _entries.Count == 0;

    public int Size => _entries.Count;
}
