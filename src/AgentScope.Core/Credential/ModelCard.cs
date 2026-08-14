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

namespace AgentScope.Core.Credential;

/// <summary>
/// 模型目录记录，描述一个模型的基本信息。
/// </summary>
public class ModelCard
{
    /// <summary>模型标识符</summary>
    public string ModelId { get; set; } = "";

    /// <summary>显示名称</summary>
    public string DisplayName { get; set; } = "";

    /// <summary>所属提供程序标识符</summary>
    public string ProviderId { get; set; } = "";

    /// <summary>上下文窗口大小（Token 数）</summary>
    public int ContextWindow { get; set; }

    /// <summary>描述</summary>
    public string? Description { get; set; }

    /// <summary>支持的模型能力标签，例如 "chat", "vision", "tool-use"</summary>
    public List<string> Capabilities { get; set; } = new();

    /// <summary>是否支持流式输出</summary>
    public bool SupportsStreaming { get; set; } = true;

    /// <summary>是否支持函数调用 / 工具使用</summary>
    public bool SupportsToolUse { get; set; } = true;
}
