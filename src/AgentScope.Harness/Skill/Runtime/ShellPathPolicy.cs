namespace AgentScope.Harness.Skill.Runtime;

/// <summary>Shell 路径策略，根据模式解析 filesRoot，对应 Java ShellPathPolicy</summary>
public sealed class ShellPathPolicy
{
    public Mode ShellMode { get; }

    private ShellPathPolicy(Mode mode) { ShellMode = mode; }

    public static ShellPathPolicy NoShell() => new(Mode.NoShell);
    public static ShellPathPolicy Sandbox() => new(Mode.Sandbox);
    public static ShellPathPolicy SandboxWithPrefix(string workspacePrefix) => new(Mode.Sandbox);
    public static ShellPathPolicy LocalWithShell(string workspaceRoot) => new(Mode.LocalWithShell);

    public string? Resolve(string skillName, string? stageResult)
    {
        return ShellMode switch
        {
            Mode.NoShell => null,
            Mode.Sandbox => $"/workspace/.skills/{skillName}",
            Mode.LocalWithShell => Path.Combine(
                Environment.CurrentDirectory, ".skills", skillName),
            _ => null
        };
    }

    public enum Mode { NoShell, Sandbox, LocalWithShell }
}
