using System.Diagnostics;
using AgentScope.Core.Message;

namespace AgentScope.Tracing.OpenTelemetry;

/// <summary>
/// 从 AgentScope 模型对象提取 OTel span 属性的工具。对标 Java AttributesExtractors。
/// </summary>
public static class AttributesExtractors
{
    public static void ExtractModelRequest(Activity activity, Msg message)
    {
        activity?.SetTag(GenAiAttributes.OperationName, "chat");
        activity?.SetTag(GenAiAttributes.ResponseModel, message.Role);
    }

    public static void ExtractToolCall(Activity activity, ToolUseBlock toolUse)
    {
        activity?.SetTag(GenAiAttributes.ToolName, toolUse.Name);
        activity?.SetTag(GenAiAttributes.ToolCallId, toolUse.Id);
    }
}
