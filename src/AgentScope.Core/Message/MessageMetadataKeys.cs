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

namespace AgentScope.Core.Message;

/// <summary>
/// Predefined message metadata key constants used throughout the AgentScope framework.
/// These keys are used to store and retrieve metadata from Msg.Metadata dictionaries.
/// Corresponds to Java: io.agentscope.core.message.MessageMetadataKeys
/// 预定义的消息元数据键名常量，在整个 AgentScope 框架中使用。
/// 这些键用于在 Msg.Metadata 字典中存储和检索元数据。
/// 对应 Java: io.agentscope.core.message.MessageMetadataKeys
/// </summary>
public static class MessageMetadataKeys
{
    /// <summary>
    /// Message type identifier (e.g., "text", "tool_call", "tool_result").
    /// 消息类型标识（例如 "text"、"tool_call"、"tool_result"）。
    /// </summary>
    public const string MessageType = "message_type";

    /// <summary>
    /// Message sub-type for finer-grained categorization.
    /// 消息子类型，用于更细粒度的分类。
    /// </summary>
    public const string SubType = "sub_type";

    /// <summary>
    /// Session ID associated with the message.
    /// 与消息关联的会话 ID。
    /// </summary>
    public const string SessionId = "session_id";

    /// <summary>
    /// Reason why generation stopped (see GenerateReason enum).
    /// 生成停止原因（参见 GenerateReason 枚举）。
    /// </summary>
    public const string GenerateReason = "generate_reason";

    /// <summary>
    /// Name of the source agent that sent the message.
    /// 发送消息的源 Agent 名称。
    /// </summary>
    public const string SourceAgent = "source_agent";

    /// <summary>
    /// Name of the target agent the message is intended for.
    /// 消息的目标 Agent 名称。
    /// </summary>
    public const string TargetAgent = "target_agent";

    /// <summary>
    /// Message priority level.
    /// 消息优先级。
    /// </summary>
    public const string Priority = "priority";

    /// <summary>
    /// List of tags associated with the message.
    /// 与消息关联的标签列表。
    /// </summary>
    public const string Tags = "tags";

    /// <summary>
    /// Tool call unique identifier.
    /// 工具调用唯一标识符。
    /// </summary>
    public const string ToolCallId = "tool_call_id";

    /// <summary>
    /// Name of the tool being called.
    /// 被调用的工具名称。
    /// </summary>
    public const string ToolName = "tool_name";

    /// <summary>
    /// Current state of the tool call (see ToolCallState enum).
    /// 工具调用的当前状态（参见 ToolCallState 枚举）。
    /// </summary>
    public const string ToolCallState = "tool_call_state";

    /// <summary>
    /// Result state of the tool execution (see ToolResultState enum).
    /// 工具执行的结果状态（参见 ToolResultState 枚举）。
    /// </summary>
    public const string ToolResultState = "tool_result_state";

    /// <summary>
    /// Tool execution duration in milliseconds.
    /// 工具执行耗时（毫秒）。
    /// </summary>
    public const string ToolDurationMs = "tool_duration_ms";

    /// <summary>
    /// Name of the model used for generation.
    /// 用于生成的模型名称。
    /// </summary>
    public const string ModelName = "model_name";

    /// <summary>
    /// Model provider (e.g., "OpenAI", "Anthropic", "DeepSeek").
    /// 模型供应商（例如 "OpenAI"、"Anthropic"、"DeepSeek"）。
    /// </summary>
    public const string ModelProvider = "model_provider";

    /// <summary>
    /// Number of input tokens consumed.
    /// 消耗的输入 token 数量。
    /// </summary>
    public const string InputTokens = "input_tokens";

    /// <summary>
    /// Number of output tokens generated.
    /// 生成的输出 token 数量。
    /// </summary>
    public const string OutputTokens = "output_tokens";

    /// <summary>
    /// Total number of tokens (input + output).
    /// 总 token 数量（输入 + 输出）。
    /// </summary>
    public const string TotalTokens = "total_tokens";

    /// <summary>
    /// Error message text (if an error occurred).
    /// 错误信息文本（如果发生错误）。
    /// </summary>
    public const string ErrorMessage = "error_message";

    /// <summary>
    /// Error code for categorizing errors.
    /// 错误代码，用于错误分类。
    /// </summary>
    public const string ErrorCode = "error_code";
}
