namespace AgentScope.Harness.Workspace;

/// <summary>
/// 路径安全策略。对标 Java PathPolicy。
/// 用于 LocalFsMode.Rooted 模式，限制允许的绝对路径根目录。
/// </summary>
public sealed class PathPolicy(IReadOnlySet<string> allowedRoots, IReadOnlySet<string>? denied = null)
{
    public IReadOnlySet<string> AllowedRoots { get; } = allowedRoots;
    public IReadOnlySet<string> Denied { get; } = denied ?? new HashSet<string>();

    public void EnsureAllowed(string path)
    {
        var full = Path.GetFullPath(path);

        if (Denied.Any(d => full.StartsWith(d, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException($"路径被策略禁止: {path}");

        if (!AllowedRoots.Any(r => full.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException($"路径超出允许根目录: {path}");
    }

    public static PathPolicy FromWorkspace(string workspaceRoot) =>
        new(new HashSet<string>([workspaceRoot]));
}
