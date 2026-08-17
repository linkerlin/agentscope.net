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

namespace AgentScope.Core.Model;

/// <summary>
/// 结构化输出提示类型，用于在生成时提醒模型使用工具或系统提示等。
/// </summary>
public class StructuredOutputReminder
{
    public string Kind { get; }

    private StructuredOutputReminder(string kind)
    {
        Kind = kind;
    }

    public static StructuredOutputReminder ToolChoice { get; } = new("tool_choice");
    public static StructuredOutputReminder SystemPrompt { get; } = new("system_prompt");
}
