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

using AgentScope.Core.AgUI.Model;

namespace AgentScope.Core.AgUI.Converter;

/// <summary>
/// AG-UI tool converter, provides helper methods to create AG-UI tool definitions.
/// AG-UI 工具转换器，提供创建 AG-UI 工具定义的辅助方法。
/// Corresponds to Java: AguiToolConverter
/// </summary>
public static class AguiToolConverter
{
    /// <summary>
    /// Creates an <see cref="AguiTool"/> from the given name, description, and optional JSON schema.
    /// 根据给定的名称、描述和可选的 JSON Schema 创建 <see cref="AguiTool"/>。
    /// </summary>
    /// <param name="name">The tool name / 工具名称</param>
    /// <param name="description">The tool description / 工具描述</param>
    /// <param name="schema">Optional JSON schema for the tool parameters / 可选的工具参数 JSON Schema</param>
    /// <returns>A new AG-UI tool instance / 新的 AG-UI 工具实例</returns>
    public static AguiTool ToAguiTool(string name, string description, object? schema = null) =>
        new(name, description, schema);
}
