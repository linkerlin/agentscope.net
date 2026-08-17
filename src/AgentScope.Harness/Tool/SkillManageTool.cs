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
using AgentScope.Extensions.Skill;

namespace AgentScope.Harness.Tool;

public sealed class SkillManageTool(ISkillRepository skillRepo) : ITool
{
    public string Name => "skill_manage";
    public string Description => "技能管理：列出、获取技能";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var action = parameters.GetValueOrDefault("action")?.ToString();
        return action switch
        {
            "list" => await ListSkillsAsync(),
            "get" => await GetSkillAsync(parameters),
            _ => ToolResult.Fail($"未知操作: {action}")
        };
    }

    private async Task<ToolResult> ListSkillsAsync()
    {
        var names = await skillRepo.GetAllSkillNamesAsync();
        return ToolResult.Ok(string.Join("\n", names));
    }

    private async Task<ToolResult> GetSkillAsync(Dictionary<string, object> p)
    {
        var name = p.GetValueOrDefault("name")?.ToString() ?? "";
        var skill = await skillRepo.GetSkillAsync(name);
        return skill != null
            ? ToolResult.Ok($"{skill.Name}: {skill.Description}\n{skill.Content}")
            : ToolResult.Fail($"技能 '{name}' 不存在");
    }

    public Dictionary<string, object> GetSchema() => new()
    {
        ["name"] = Name,
        ["description"] = Description,
        ["parameters"] = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["action"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "操作: list/get" },
                ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "技能名称" }
            },
            ["required"] = new[] { "action" }
        }
    };
}
