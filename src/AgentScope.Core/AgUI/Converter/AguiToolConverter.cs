using AgentScope.Core.AgUI.Model;

namespace AgentScope.Core.AgUI.Converter;

/// <summary>
/// AG-UI 工具转换器。对标 Java AguiToolConverter。
/// </summary>
public static class AguiToolConverter
{
    public static AguiTool ToAguiTool(string name, string description, object? schema = null) =>
        new(name, description, schema);
}
