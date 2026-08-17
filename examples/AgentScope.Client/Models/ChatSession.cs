using System.ComponentModel.DataAnnotations;

namespace AgentScope.Client.Models;

public class ChatSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? Title { get; set; }

    public Guid? AgentConfigId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}
