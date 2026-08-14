namespace AgentScope.Extensions.Nacos;

/// <summary>
/// Nacos A2A 注册表配置选项。对标 Java NacosA2aRegistryProperties。
/// 通过 IOptions 模式注入。
/// </summary>
public sealed class NacosA2aRegistryOptions
{
    public string ServerAddr { get; set; } = "http://localhost:8848";
    public string Namespace { get; set; } = "";
    public string GroupName { get; set; } = "DEFAULT_GROUP";
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);
}
