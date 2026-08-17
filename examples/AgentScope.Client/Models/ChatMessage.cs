using System.ComponentModel.DataAnnotations;

namespace AgentScope.Client.Models;

public class ChatMessage
{
    [Key]
    public long Id { get; set; }

    public Guid SessionId { get; set; }

    public string Role { get; set; } = string.Empty;

    public string? Content { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public int? TokenCount { get; set; }
}
