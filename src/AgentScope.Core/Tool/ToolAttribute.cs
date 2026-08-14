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
/// 标记方法为 Agent 可用工具，对应 Java @Tool 注解
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ToolAttribute : Attribute
{
    /// <summary>工具名称（默认使用方法名）</summary>
    public string? Name { get; init; }

    /// <summary>工具描述</summary>
    public string? Description { get; init; }

    /// <summary>是否启用严格 JSON Schema 模式</summary>
    public bool Strict { get; init; }

    /// <summary>是否为只读工具（不会产生副作用）</summary>
    public bool ReadOnly { get; init; }

    /// <summary>是否为外部工具（需额外配置）</summary>
    public bool ExternalTool { get; init; }
}

/// <summary>
/// 标记工具方法参数，对应 Java @ToolParam 注解
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public class ToolParamAttribute : Attribute
{
    /// <summary>参数名称（默认使用参数名）</summary>
    public string? Name { get; init; }

    /// <summary>参数描述</summary>
    public string? Description { get; init; }

    /// <summary>是否必需</summary>
    public bool Required { get; init; } = true;
}
