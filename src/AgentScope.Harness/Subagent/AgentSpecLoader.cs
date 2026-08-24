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

namespace AgentScope.Harness.Subagent;

/// <summary>
/// Agent spec loader. Loads SubagentDeclaration from Markdown files with YAML front matter.
/// 子 Agent 配置加载器。从 Markdown 文件（含 YAML front matter）加载 SubagentDeclaration。
/// </summary>
public static class AgentSpecLoader
{
    /// <summary>
    /// Loads a subagent declaration by spec reference.
    /// 通过 spec 引用加载子 Agent 声明。
    /// </summary>
    /// <param name="specRef">The spec file name or agent name / 规格文件名或 Agent 名称</param>
    /// <returns>The parsed SubagentDeclaration / 解析后的声明</returns>
    public static SubagentDeclaration Load(string specRef)
    {
        // 简单实现：从文件系统加载 Markdown 文件
        // Simple implementation: load Markdown file from filesystem
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "subagents", specRef);
        if (File.Exists(path))
        {
            var content = File.ReadAllText(path);
            return Parse(content, specRef);
        }

        // 按名称直接创建
        // Create directly by name
        return new SubagentDeclaration(specRef, $"Subagent: {specRef}");
    }

    /// <summary>
    /// Parses a Markdown string with YAML front matter into a SubagentDeclaration.
    /// 解析包含 YAML front matter 的 Markdown 字符串为 SubagentDeclaration。
    /// </summary>
    /// <param name="markdown">The raw Markdown content / 原始 Markdown 内容</param>
    /// <param name="name">The agent name / Agent 名称</param>
    /// <returns>The parsed SubagentDeclaration / 解析后的声明</returns>
    public static SubagentDeclaration Parse(string markdown, string name)
    {
        // YAML front matter 解析示例
        // YAML front matter parsing example
        // 格式: ---\nkey: value\n---\nbody
        // Format: ---\nkey: value\n---\nbody
        var name_ = name;

        // 检查是否以 --- 开头（YAML front matter 标记）
        // Check if starts with --- (YAML front matter delimiter)
        if (markdown.StartsWith("---"))
        {
            var end = markdown.IndexOf("---", 3, StringComparison.Ordinal);
            if (end > 0)
            {
                // 提取 YAML 块和正文
                // Extract YAML block and body
                var yaml = markdown[3..end].Trim();
                var body = markdown[(end + 3)..].Trim();
                return new SubagentDeclaration(
                    Name: name_,
                    Description: yaml,
                    InlineBody: body);
            }
        }

        return new SubagentDeclaration(Name: name_, Description: markdown);
    }
}
