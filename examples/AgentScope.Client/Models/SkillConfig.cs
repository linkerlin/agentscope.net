using System.ComponentModel.DataAnnotations;

namespace AgentScope.Client.Models;

public class SkillConfig
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>来源类型：file（Markdown 文件）、inline（内嵌内容）</summary>
    public string SourceType { get; set; } = "file";

    /// <summary>Markdown 文件路径（SourceType=file 时生效）</summary>
    public string? SourcePath { get; set; }

    /// <summary>内嵌技能内容（SourceType=inline 时生效）</summary>
    public string? RawContent { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
