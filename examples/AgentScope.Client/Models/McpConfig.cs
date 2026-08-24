using System.ComponentModel.DataAnnotations;

namespace AgentScope.Client.Models;

public class McpConfig
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string TransportType { get; set; } = "stdio";

    public string? Command { get; set; }

    public string? Args { get; set; }

    public string? Url { get; set; }

    public string? ApiKey { get; set; }

    public string? WorkingDirectory { get; set; }

    public bool IsEnabled { get; set; } = true;

    /// <summary>连接状态：Disconnected/Connected/Error</summary>
    public string ConnectionStatus { get; set; } = "Disconnected";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
