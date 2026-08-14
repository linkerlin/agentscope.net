namespace AgentScope.Harness;

/// <summary>
/// 沙箱/状态隔离作用域。对标 Java IsolationScope。
/// </summary>
public enum IsolationScope
{
    Session,
    User,
    Agent,
    Global
}
