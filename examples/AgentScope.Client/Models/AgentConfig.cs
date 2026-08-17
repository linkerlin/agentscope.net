using System.ComponentModel.DataAnnotations;

namespace AgentScope.Client.Models;

public class AgentConfig
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? SystemPrompt { get; set; }

    /// <summary>关联的 LLM 配置 ID</summary>
    public Guid? ModelId { get; set; }

    /// <summary>关联的 MCP 配置 ID</summary>
    public Guid? McpId { get; set; }

    /// <summary>关联的 Skill 配置 ID</summary>
    public Guid? SkillId { get; set; }

    public int MaxTokens { get; set; } = 4096;

    public int MaxIterations { get; set; } = 10;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsEnabled { get; set; } = true;
}
