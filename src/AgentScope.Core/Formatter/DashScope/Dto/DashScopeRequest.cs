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
/// DashScope API request DTO.
/// DashScope API 请求 DTO
/// 
/// Java参考: io.agentscope.core.formatter.dashscope.dto.DashScopeRequest
/// </summary>
public class DashScopeRequest
{
    /// <summary>
    /// The model name (e.g., "qwen-plus", "qwen-vl-max")
    /// </summary>
    [JsonPropertyName("model")]
    public required string Model { get; set; }

    /// <summary>
    /// The input containing messages
    /// </summary>
    [JsonPropertyName("input")]
    public required DashScopeInput Input { get; set; }

    /// <summary>
    /// The generation parameters
    /// </summary>
    [JsonPropertyName("parameters")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DashScopeParameters? Parameters { get; set; }
}

/// <summary>
/// DashScope input DTO.
/// DashScope 输入 DTO
/// </summary>
public class DashScopeInput
{
    /// <summary>
    /// List of messages
    /// </summary>
    [JsonPropertyName("messages")]
    public required List<DashScopeMessage> Messages { get; set; }
}
