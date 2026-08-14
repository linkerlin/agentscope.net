namespace AgentScope.Tracing.OpenTelemetry;

/// <summary>
/// gen_ai.* 语义属性常量。对标 Java GenAiIncubatingAttributes。
/// 对齐 OpenTelemetry 语义约定。
/// </summary>
public static class GenAiAttributes
{
    public const string OperationName = "gen_ai.operation.name";
    public const string AgentName = "gen_ai.agent.name";
    public const string RequestModel = "gen_ai.request.model";
    public const string RequestMaxTokens = "gen_ai.request.max_tokens";
    public const string RequestTemperature = "gen_ai.request.temperature";
    public const string ResponseId = "gen_ai.response.id";
    public const string ResponseModel = "gen_ai.response.model";
    public const string UsageInputTokens = "gen_ai.usage.input_tokens";
    public const string UsageOutputTokens = "gen_ai.usage.output_tokens";
    public const string ToolName = "gen_ai.tool.name";
    public const string ToolCallId = "gen_ai.tool.call_id";
}
