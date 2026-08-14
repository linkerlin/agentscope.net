using System.Text.Json;

namespace AgentScope.Harness.Tools;

public static class ToolsConfigLoader
{
    public static async Task<ToolsConfig> LoadAsync(string path, CancellationToken ct = default)
    {
        if (!File.Exists(path)) return new ToolsConfig();
        var json = await File.ReadAllTextAsync(path, ct);
        return JsonSerializer.Deserialize<ToolsConfig>(json) ?? new ToolsConfig();
    }
}
