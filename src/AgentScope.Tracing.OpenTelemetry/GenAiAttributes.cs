// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace AgentScope.Tracing.OpenTelemetry;

/// <summary>
/// gen_ai.* 语义属性常量。对标 Java GenAiIncubatingAttributes。
/// 对齐 OpenTelemetry 语义约定。
/// </summary>
public static class GenAiAttributes
{
    /// <summary>
    /// 生成式 AI 操作名称属性键
    /// Gen AI operation name attribute key
    /// </summary>
    public const string OperationName = "gen_ai.operation.name";

    /// <summary>
    /// Agent 名称属性键
    /// Agent name attribute key
    /// </summary>
    public const string AgentName = "gen_ai.agent.name";

    /// <summary>
    /// 请求模型名称属性键
    /// Request model name attribute key
    /// </summary>
    public const string RequestModel = "gen_ai.request.model";

    /// <summary>
    /// 请求最大令牌数属性键
    /// Request max tokens attribute key
    /// </summary>
    public const string RequestMaxTokens = "gen_ai.request.max_tokens";

    /// <summary>
    /// 请求温度参数属性键
    /// Request temperature attribute key
    /// </summary>
    public const string RequestTemperature = "gen_ai.request.temperature";

    /// <summary>
    /// 响应 ID 属性键
    /// Response ID attribute key
    /// </summary>
    public const string ResponseId = "gen_ai.response.id";

    /// <summary>
    /// 响应模型名称属性键
    /// Response model name attribute key
    /// </summary>
    public const string ResponseModel = "gen_ai.response.model";

    /// <summary>
    /// 输入令牌用量属性键
    /// Input tokens usage attribute key
    /// </summary>
    public const string UsageInputTokens = "gen_ai.usage.input_tokens";

    /// <summary>
    /// 输出令牌用量属性键
    /// Output tokens usage attribute key
    /// </summary>
    public const string UsageOutputTokens = "gen_ai.usage.output_tokens";

    /// <summary>
    /// 工具名称属性键
    /// Tool name attribute key
    /// </summary>
    public const string ToolName = "gen_ai.tool.name";

    /// <summary>
    /// 工具调用 ID 属性键
    /// Tool call ID attribute key
    /// </summary>
    public const string ToolCallId = "gen_ai.tool.call_id";
}
