using System.ComponentModel.DataAnnotations;

namespace AgentScope.Client.Models;

public class LlmConfig
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Provider { get; set; } = "openai";

    public string ModelName { get; set; } = string.Empty;

    public string? ApiKey { get; set; }

    public string? BaseUrl { get; set; }

    public double Temperature { get; set; } = 0.7;

    public bool IsDefault { get; set; }
}
