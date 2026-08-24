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

using AgentScope.Core.Tool;

namespace AgentScope.Harness.Skill.Runtime;

/// <summary>技能运行时：聚合 SkillLoadTool 和 SkillPromptBuilder，对应 Java SkillRuntime</summary>
public sealed class SkillRuntime
{
    private readonly SkillPromptBuilder _promptBuilder = new();
    private SkillCatalog _catalog = SkillCatalog.Empty;

    public SkillRuntime() { }

    public SkillRuntime(IEnumerable<HarnessSkillEntry> entries)
    {
        _catalog = SkillCatalog.Of(entries);
    }

    public SkillCatalog CurrentCatalog => _catalog;

    public void Install(SkillCatalog catalog)
    {
        _catalog = catalog;
    }

    public string RenderPrompt(int? maxSkills = null)
    {
        return _promptBuilder.Render(_catalog, maxSkills);
    }

    public void PrepareToolkit(Toolkit toolkit)
    {
        var loadTool = new SkillLoadTool(_catalog.All);
        toolkit.AddTool(loadTool);
    }
}
