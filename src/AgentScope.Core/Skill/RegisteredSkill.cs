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

namespace AgentScope.Core.Skill;

/// <summary>
/// Skill 注册元数据（仓库扫描得到的条目，尚未加载为 ISkill）。
/// </summary>
public class RegisteredSkill
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<string> ToolNames { get; set; } = new();
    public bool IsActiveByDefault { get; set; } = true;
    public string? SourcePath { get; set; }
    public string? RawContent { get; set; }
}
