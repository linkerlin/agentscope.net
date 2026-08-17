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

namespace AgentScope.Core.Agent;

/// <summary>
/// 流式调用选项，控制是否包含推理/工具调用等事件及超时、取消。
/// </summary>
public class StreamOptions
{
    /// <summary>是否包含推理阶段事件</summary>
    public bool IncludeReasoning { get; set; } = true;

    /// <summary>是否包含工具调用事件</summary>
    public bool IncludeToolCalls { get; set; } = true;

    /// <summary>超时（null 表示不限制）</summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>取消令牌</summary>
    public CancellationToken CancellationToken { get; set; }
}
