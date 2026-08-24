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
using System.Collections.Generic;

namespace AgentScope.Core.Tool;

/// <summary>
/// 工具参数 Schema 校验器：校验输入参数是否符合工具 schema（必填项 + 基本类型）。
/// 对应 Java: io.agentscope.core.tool.ToolValidator
/// </summary>
public static class ToolValidator
{
    /// <summary>
    /// 校验参数。返回错误消息列表；为空表示通过。
    /// </summary>
    public static List<string> Validate(Dictionary<string, object> schema, IReadOnlyDictionary<string, object>? arguments)
    {
        var errors = new List<string>();
        if (schema == null)
        {
            return errors;
        }

        arguments ??= new Dictionary<string, object>();

        if (!schema.TryGetValue("parameters", out var parametersObj) ||
            parametersObj is not Dictionary<string, object> parameters)
        {
            return errors;
        }

        // 必填项
        if (parameters.TryGetValue("required", out var reqObj) && reqObj is IList<string> requiredList)
        {
            foreach (var req in requiredList)
            {
                if (!arguments.ContainsKey(req))
                {
                    errors.Add($"缺少必填参数: {req}");
                }
            }
        }
        else if (parameters.TryGetValue("required", out var reqObj2) && reqObj2 is IList<object> requiredObjs)
        {
            foreach (var req in requiredObjs)
            {
                var name = req?.ToString();
                if (name != null && !arguments.ContainsKey(name))
                {
                    errors.Add($"缺少必填参数: {name}");
                }
            }
        }

        // 类型基本校验
        if (parameters.TryGetValue("properties", out var propsObj) &&
            propsObj is Dictionary<string, object> properties)
        {
            foreach (var kv in arguments)
            {
                if (!properties.TryGetValue(kv.Key, out var propObj) ||
                    propObj is not Dictionary<string, object> prop)
                {
                    continue;
                }

                var expectedType = prop.TryGetValue("type", out var t) ? t?.ToString() : null;
                if (expectedType == null || kv.Value == null)
                {
                    continue;
                }

                if (!TypeMatches(kv.Value, expectedType))
                {
                    errors.Add($"参数 {kv.Key} 类型不符，期望 {expectedType}。");
                }
            }
        }

        return errors;
    }

    private static bool TypeMatches(object value, string expectedType)
    {
        try
        {
            return expectedType switch
            {
                "string" => value is string || value is char,
                "integer" => value is int or long or short or byte,
                "number" => value is double or float or decimal or int or long,
                "boolean" => value is bool,
                "array" => value is Array || value.GetType().Name == "List`1" ||
                           value.GetType().FullName?.StartsWith("System.Collections.Generic.List`1") == true,
                "object" => true,
                _ => true
            };
        }
        catch
        {
            return true;
        }
    }
}
