namespace AgentScope.Harness.Workspace;

/// <summary>PLAN/BUILD 模式管理器，对应 Java PlanModeManager</summary>
public sealed class PlanModeManager
{
    public PlanMode CurrentMode { get; private set; } = PlanMode.Build;

    public event Action<PlanMode>? OnModeChanged;

    public void SetMode(PlanMode mode)
    {
        if (CurrentMode == mode) return;
        CurrentMode = mode;
        OnModeChanged?.Invoke(mode);
    }

    public void Toggle()
    {
        SetMode(CurrentMode == PlanMode.Plan ? PlanMode.Build : PlanMode.Plan);
    }
}

public enum PlanMode
{
    Plan,
    Build
}
