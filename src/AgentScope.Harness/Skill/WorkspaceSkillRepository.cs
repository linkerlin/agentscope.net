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

using AgentScope.Core.Skill;

namespace AgentScope.Harness.Skill;

/// <summary>
/// Workspace skill repository: scans .md skill files under the specified workspace directory and loads them on demand.
/// 工作区技能仓库：扫描指定工作区目录下的 .md 技能文件并按需加载。
/// </summary>
public class WorkspaceSkillRepository : ISkillRepository
{
    private readonly string _workspaceRoot;
    private readonly string _skillsDir;
    private readonly MarkdownSkillParser _parser = new();

    /// <summary>
    /// Initializes a new instance of <see cref="WorkspaceSkillRepository"/>.
    /// 初始化 <see cref="WorkspaceSkillRepository"/> 的新实例。
    /// </summary>
    /// <param name="workspaceRoot">Workspace root path / 工作区根路径。</param>
    /// <param name="skillsDir">Skills directory relative to workspace root / 相对于工作区根目录的技能目录。</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="workspaceRoot"/> is null.</exception>
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

    /// <summary>
    /// Adapts <see cref="RegisteredSkill"/> into a minimal usable <see cref="ISkill"/>.
    /// 把 RegisteredSkill 适配为最小可用 ISkill。
    /// </summary>
    private sealed class MarkdownSkillAdapter : ISkill
    {
        private readonly RegisteredSkill _r;
        public MarkdownSkillAdapter(RegisteredSkill r) => _r = r;
        /// <inheritdoc />
        public string Id => _r.Id;
        /// <inheritdoc />
        public string Name => _r.Name;
        /// <inheritdoc />
        public string Description => _r.Description;
        /// <inheritdoc />
        public IReadOnlyList<Core.Tool.ITool> Tools { get; } = Array.Empty<Core.Tool.ITool>();
        /// <inheritdoc />
        public bool IsActive { get; set; }
    }
}
