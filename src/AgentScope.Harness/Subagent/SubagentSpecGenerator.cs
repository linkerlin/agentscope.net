namespace AgentScope.Harness.Subagent;

/// <summary>子代理规格生成器，对应 Java SubagentSpecGenerator</summary>
public sealed class SubagentSpecGenerator
{
    private static readonly string PromptTemplate = @"
根据以下描述生成子代理规格：
描述: {0}
已有 Agent ID: {1}
请以 Markdown 格式输出。";

    public async Task<string> GenerateMarkdownAsync(string description,
        ICollection<string> existingIds)
    {
        return string.Format(PromptTemplate, description,
            string.Join(", ", existingIds));
    }

    public async Task<GeneratedSpec?> GenerateAndValidateAsync(
        string description, string agentName,
        ICollection<string> existingIds)
    {
        var markdown = await GenerateMarkdownAsync(description, existingIds);
        return new GeneratedSpec(markdown, null);
    }

    public sealed record GeneratedSpec(string Markdown,
        SubagentDeclaration? Declaration);
}
