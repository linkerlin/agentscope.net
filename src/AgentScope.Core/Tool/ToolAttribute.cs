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

using System;

namespace AgentScope.Core.Tool;

/// <summary>
/// Marks a method as an agent-available tool, corresponding to the Java @Tool annotation.
/// 标记方法为 Agent 可用工具，对应 Java @Tool 注解。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ToolAttribute : Attribute
{
    /// <summary>
    /// Tool name (defaults to the method name).
    /// 工具名称（默认使用方法名）。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Tool description for LLM consumption.
    /// 工具描述，用于 LLM 理解工具用途。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether to enable strict JSON Schema mode for parameter validation.
    /// 是否启用严格 JSON Schema 模式进行参数校验。
    /// </summary>
    public bool Strict { get; init; }

    /// <summary>
    /// Whether this is a read-only tool (no side effects).
    /// 是否为只读工具（不会产生副作用）。
    /// </summary>
    public bool ReadOnly { get; init; }

    /// <summary>
    /// Whether this is an external tool requiring additional configuration.
    /// 是否为外部工具（需额外配置）。
    /// </summary>
    public bool ExternalTool { get; init; }
}

/// <summary>
/// Marks a tool method parameter, corresponding to the Java @ToolParam annotation.
/// 标记工具方法参数，对应 Java @ToolParam 注解。
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class ToolParamAttribute : Attribute
{
    /// <summary>
    /// Parameter name (defaults to the parameter name).
    /// 参数名称（默认使用参数名）。
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Parameter description for LLM consumption.
    /// 参数描述，用于 LLM 理解参数含义。
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether the parameter is required (default true).
    /// 是否必需（默认为 true）。
    /// </summary>
    public bool Required { get; init; } = true;
}
