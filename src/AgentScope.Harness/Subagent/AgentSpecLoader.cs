namespace AgentScope.Harness.Subagent;

/// <summary>
/// 子 Agent 配置加载器。对标 Java AgentSpecLoader。
/// 从 Markdown 文件（含 YAML front matter）加载 SubagentDeclaration。
/// </summary>
public static class AgentSpecLoader
{
    public static SubagentDeclaration Load(string specRef)
    {
        // 简单实现：从文件系统加载 Markdown 文件
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "subagents", specRef);
        if (File.Exists(path))
        {
            var content = File.ReadAllText(path);
            return Parse(content, specRef);
        }

        // 按名称直接创建
        return new SubagentDeclaration(specRef, $"Subagent: {specRef}");
    }

    public static SubagentDeclaration Parse(string markdown, string name)
    {
        // YAML front matter 解析示例
        // 格式: ---\nkey: value\n---\nbody
        var name_ = name;

        if (markdown.StartsWith("---"))
        {
            var end = markdown.IndexOf("---", 3, StringComparison.Ordinal);
            if (end > 0)
            {
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
