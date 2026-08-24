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

namespace AgentScope.Core.A2A.Client.Message;

/// <summary>
/// Constants for A2A message conversion. Corresponds to Java MessageConstants.
/// A2A 消息转换常量。对标 Java MessageConstants。
/// </summary>
public static class MessageConstants
{
    /// <summary>Content block type: plain text / 内容块类型：纯文本</summary>
    public const string BlockTypeText = "text";
    /// <summary>Content block type: thinking/chain-of-thought / 内容块类型：思考过程</summary>
    public const string BlockTypeThinking = "thinking";
    /// <summary>Content block type: image / 内容块类型：图片</summary>
    public const string BlockTypeImage = "image";
    /// <summary>Content block type: audio / 内容块类型：音频</summary>
    public const string BlockTypeAudio = "audio";
    /// <summary>Content block type: video / 内容块类型：视频</summary>
    public const string BlockTypeVideo = "video";
    /// <summary>Content block type: tool use request / 内容块类型：工具使用请求</summary>
    public const string BlockTypeToolUse = "tool_use";
    /// <summary>Content block type: tool execution result / 内容块类型：工具执行结果</summary>
    public const string BlockTypeToolResult = "tool_result";

    /// <summary>Metadata key: message source identifier / 元数据键：消息来源标识</summary>
    public const string MetaMsgSource = "_agentscope_msg_source";
    /// <summary>Metadata key: message unique ID / 元数据键：消息唯一 ID</summary>
    public const string MetaMsgId = "_agentscope_msg_id";
    /// <summary>Metadata key: content block type discriminator / 元数据键：内容块类型标识</summary>
    public const string MetaBlockType = "_agentscope_block_type";
    /// <summary>Metadata key: tool name / 元数据键：工具名称</summary>
    public const string MetaToolName = "_agentscope_tool_name";
    /// <summary>Metadata key: tool call ID / 元数据键：工具调用 ID</summary>
    public const string MetaToolCallId = "_agentscope_tool_call_id";

    /// <summary>A2A Task state: submitted / A2A 任务状态：已提交</summary>
    public const string TaskStateSubmitted = "submitted";
    /// <summary>A2A Task state: working / A2A 任务状态：执行中</summary>
    public const string TaskStateWorking = "working";
    /// <summary>A2A Task state: awaiting user input / A2A 任务状态：等待用户输入</summary>
    public const string TaskStateInputRequired = "input-required";
    /// <summary>A2A Task state: completed / A2A 任务状态：已完成</summary>
    public const string TaskStateCompleted = "completed";
    /// <summary>A2A Task state: canceled / A2A 任务状态：已取消</summary>
    public const string TaskStateCanceled = "canceled";
    /// <summary>A2A Task state: failed / A2A 任务状态：失败</summary>
    public const string TaskStateFailed = "failed";
    /// <summary>A2A Task state: unknown / A2A 任务状态：未知</summary>
    public const string TaskStateUnknown = "unknown";
}
