namespace AgentScope.Harness.Workspace;

public static class WorkspacePathNormalizer
{
    /// <summary>规范化路径：统一分隔符、去除 ..</summary>
    public static string Normalize(string path, string workspaceRoot)
    {
        var full = Path.GetFullPath(Path.Combine(workspaceRoot, path));
        // 安全检查：确保在 workspaceRoot 内
        if (!full.StartsWith(Path.GetFullPath(workspaceRoot), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal detected: {path}");
        return full;
    }

    /// <summary>获取相对于工作区的路径</summary>
    public static string RelativeTo(string fullPath, string workspaceRoot)
    {
        var root = Path.GetFullPath(workspaceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var full = Path.GetFullPath(fullPath);
        return full.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? full[root.Length..]
            : fullPath;
    }
}
