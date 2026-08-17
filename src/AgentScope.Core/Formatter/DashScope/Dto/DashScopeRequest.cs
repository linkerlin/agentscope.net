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

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AgentScope.Core.Formatter.DashScope.Dto;

/// <summary>
/// DashScope API request DTO, the top-level request structure.
/// DashScope API 请求 DTO，顶层请求结构体。
///
/// 包含模型名称、输入消息和生成参数。
/// Contains the model name, input messages, and generation parameters.
///
/// Java参考: io.agentscope.core.formatter.dashscope.dto.DashScopeRequest
/// </summary>
public class DashScopeRequest
{
    /// <summary>
    /// 模型名称（如 "qwen-plus", "qwen-vl-max"）。
    /// The model name (e.g., "qwen-plus", "qwen-vl-max").
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    /// <summary>
    /// 包含消息列表的输入。
    /// The input containing the list of messages.
    /// </summary>
    [JsonPropertyName("input")]
    public required DashScopeInput Input { get; set; }

    /// <summary>
    /// 生成参数配置，包括温度、采样参数和工具等。
    /// The generation parameters including temperature, sampling params, and tools.
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DashScopeParameters? Parameters { get; set; }
}

/// <summary>
/// DashScope input DTO, wrapping the message list.
/// DashScope 输入 DTO，包装消息列表。
/// </summary>
public class DashScopeInput
{
    /// <summary>
    /// 消息列表，构成对话上下文。
    /// List of messages forming the conversation context.
    /// </summary>
    [JsonPropertyName("messages")]
    public required List<DashScopeMessage> Messages { get; set; }
}
