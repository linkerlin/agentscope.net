namespace AgentScope.Harness.Workspace;

/// <summary>
/// 本地文件系统路径隔离模式。对标 Java LocalFsMode。
/// </summary>
public enum LocalFsMode
{
    /// <summary>所有路径锚定到根目录，拒绝 .. 遍历</summary>
    Sandboxed,
    /// <summary>绝对路径仅允许在白名单根目录下</summary>
    Rooted,
    /// <summary>绝对路径原样通过（限信任 Agent）</summary>
    Unrestricted
}
