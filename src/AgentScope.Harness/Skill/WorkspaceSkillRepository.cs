// Copyright 2024-2026 the original author or authors.
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

using AgentScope.Core.Skill;

namespace AgentScope.Harness.Skill;

/// <summary>
/// 工作区技能仓库：扫描指定工作区目录下的 .md 技能文件并按需加载。
/// 对应 Java: io.agentscope.harness.agent.skill.WorkspaceSkillRepository
/// </summary>
public class WorkspaceSkillRepository : ISkillRepository
{
    private readonly string _workspaceRoot;
    private readonly string _skillsDir;
    private readonly MarkdownSkillParser _parser = new();

    public WorkspaceSkillRepository(string workspaceRoot, string skillsDir = ".agentscope/skills")
    {
        _workspaceRoot = workspaceRoot ?? throw new ArgumentNullException(nameof(workspaceRoot));
        _skillsDir = skillsDir;
    }

    /// <inheritdoc />
    public IEnumerable<RegisteredSkill> Scan()
    {
        var dir = Path.IsPathRooted(_skillsDir)
            ? _skillsDir
            : Path.Combine(_workspaceRoot, _skillsDir);

        if (!Directory.Exists(dir))
        {
            return Array.Empty<RegisteredSkill>();
        }

        var result = new List<RegisteredSkill>();
        foreach (var file in Directory.GetFiles(dir, "*.md", SearchOption.AllDirectories))
        {
            try
            {
                result.Add(_parser.ParseFile(file));
            }
            catch
            {
                // 解析失败的文件跳过
            }
        }

        return result;
    }

    /// <inheritdoc />
    public ISkill Load(RegisteredSkill registered)
    {
        if (registered == null) throw new ArgumentNullException(nameof(registered));
        return new MarkdownSkillAdapter(registered);
    }

    /// <summary>把 RegisteredSkill 适配为最小可用 ISkill。</summary>
    private sealed class MarkdownSkillAdapter : ISkill
    {
        private readonly RegisteredSkill _r;
        public MarkdownSkillAdapter(RegisteredSkill r) => _r = r;
        public string Id => _r.Id;
        public string Name => _r.Name;
        public string Description => _r.Description;
        public IReadOnlyList<Core.Tool.ITool> Tools { get; } = Array.Empty<Core.Tool.ITool>();
        public bool IsActive { get; set; }
    }
}
