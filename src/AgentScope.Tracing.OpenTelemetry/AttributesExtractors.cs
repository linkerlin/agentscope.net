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

using System.Diagnostics;
using AgentScope.Core.Message;

namespace AgentScope.Tracing.OpenTelemetry;

/// <summary>
/// 从 AgentScope 模型对象提取 OTel span 属性的工具。对标 Java AttributesExtractors。
/// </summary>
public static class AttributesExtractors
{
    /// <summary>
    /// 从模型请求消息中提取 span 属性
    /// Extracts span attributes from a model request message
    /// </summary>
    /// <param name="activity">要标记的 Activity 实例 / The Activity instance to tag</param>
    /// <param name="message">模型请求消息 / The model request message</param>
    public static void ExtractModelRequest(Activity activity, Msg message)
    {
        activity?.SetTag(GenAiAttributes.OperationName, "chat");
        activity?.SetTag(GenAiAttributes.ResponseModel, message.Role);
    }

    /// <summary>
    /// 从工具调用块中提取 span 属性
    /// Extracts span attributes from a tool call block
    /// </summary>
    /// <param name="activity">要标记的 Activity 实例 / The Activity instance to tag</param>
    /// <param name="toolUse">工具调用块 / The tool use block</param>
    public static void ExtractToolCall(Activity activity, ToolUseBlock toolUse)
    {
        activity?.SetTag(GenAiAttributes.ToolName, toolUse.Name);
        activity?.SetTag(GenAiAttributes.ToolCallId, toolUse.Id);
    }
}
