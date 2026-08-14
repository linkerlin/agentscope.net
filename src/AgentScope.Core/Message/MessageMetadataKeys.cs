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
/// 预定义的消息元数据键名常量
/// </summary>
public static class MessageMetadataKeys
{
    /// <summary>
    /// 消息类型标识
    /// </summary>
    public const string MessageType = "message_type";

    /// <summary>
    /// 消息子类型
    /// </summary>
    public const string SubType = "sub_type";

    /// <summary>
    /// 会话 ID
    /// </summary>
    public const string SessionId = "session_id";

    /// <summary>
    /// 生成停止原因
    /// </summary>
    public const string GenerateReason = "generate_reason";

    /// <summary>
    /// 消息来源 Agent
    /// </summary>
    public const string SourceAgent = "source_agent";

    /// <summary>
    /// 消息目标 Agent
    /// </summary>
    public const string TargetAgent = "target_agent";

    /// <summary>
    /// 消息优先级
    /// </summary>
    public const string Priority = "priority";

    /// <summary>
    /// 消息标签列表
    /// </summary>
    public const string Tags = "tags";

    /// <summary>
    /// 工具调用 ID
    /// </summary>
    public const string ToolCallId = "tool_call_id";

    /// <summary>
    /// 工具名称
    /// </summary>
    public const string ToolName = "tool_name";

    /// <summary>
    /// 工具调用状态
    /// </summary>
    public const string ToolCallState = "tool_call_state";

    /// <summary>
    /// 工具结果状态
    /// </summary>
    public const string ToolResultState = "tool_result_state";

    /// <summary>
    /// 工具执行耗时（毫秒）
    /// </summary>
    public const string ToolDurationMs = "tool_duration_ms";

    /// <summary>
    /// 模型名称
    /// </summary>
    public const string ModelName = "model_name";

    /// <summary>
    /// 模型供应商
    /// </summary>
    public const string ModelProvider = "model_provider";

    /// <summary>
    /// 输入 token 数量
    /// </summary>
    public const string InputTokens = "input_tokens";

    /// <summary>
    /// 输出 token 数量
    /// </summary>
    public const string OutputTokens = "output_tokens";

    /// <summary>
    /// 总 token 数量
    /// </summary>
    public const string TotalTokens = "total_tokens";

    /// <summary>
    /// 错误信息
    /// </summary>
    public const string ErrorMessage = "error_message";

    /// <summary>
    /// 错误代码
    /// </summary>
    public const string ErrorCode = "error_code";
}
