namespace AgentScope.Harness.Filesystem.Remote;

/// <summary>KV 存储接口，对应 Java IBaseStore</summary>
public interface IBaseStore
{
    Task<string?> GetAsync(string key, CancellationToken ct = default);
    Task SetAsync(string key, string value, CancellationToken ct = default);
    Task<bool> DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}
