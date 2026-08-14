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
using System.Reflection;

namespace AgentScope.Core.Tool;

/// <summary>
/// 从 .NET 类型/方法反射生成 JSON Schema（function-calling 参数 schema）。
/// 相比 Toolkit 内置的简易生成器，支持枚举、嵌套对象、数组、可空与必填项。
/// 对应 Java: io.agentscope.core.tool.ToolSchemaGenerator
/// </summary>
public static class ToolSchemaGenerator
{
    /// <summary>
    /// 为方法生成工具 schema（含 name/description/parameters）。
    /// </summary>
    public static Dictionary<string, object> ForMethod(MethodInfo method, string? name = null, string? description = null)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));

        var toolAttr = method.GetCustomAttribute<ToolAttribute>();
        var toolName = name ?? toolAttr?.Name ?? method.Name;
        var desc = description ?? toolAttr?.Description ?? $"[Tool] {method.DeclaringType?.Name}.{method.Name}";

        var (properties, required) = BuildParameters(method.GetParameters());

        return new Dictionary<string, object>
        {
            ["name"] = toolName,
            ["description"] = desc,
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = properties,
                ["required"] = required
            }
        };
    }

    /// <summary>
    /// 为任意类型生成 JSON Schema 片段（仅 type/描述部分，无 name/description 包装）。
    /// </summary>
    public static Dictionary<string, object> ForType(Type type, string? description = null)
    {
        var schema = BuildTypeSchema(type);
        if (!string.IsNullOrEmpty(description))
        {
            schema["description"] = description!;
        }

        return schema;
    }

    private static (Dictionary<string, object> properties, List<string> required) BuildParameters(ParameterInfo[] parameters)
    {
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var p in parameters)
        {
            var paramAttr = p.GetCustomAttribute<ToolParamAttribute>();
            var name = paramAttr?.Name ?? p.Name ?? $"arg{p.Position}";
            var desc = paramAttr?.Description ?? $"参数 {name}";

            var schema = BuildTypeSchema(p.ParameterType);
            schema["description"] = desc;

            properties[name] = schema;

            var isRequired = paramAttr?.Required ?? !p.HasDefaultValue;
            if (isRequired)
            {
                required.Add(name);
            }
        }

        return (properties, required);
    }

    private static Dictionary<string, object> BuildTypeSchema(Type type)
    {
        var nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null)
        {
            type = nullable;
        }

        if (type == typeof(string) || type == typeof(char))
        {
            return new Dictionary<string, object> { ["type"] = "string" };
        }

        if (type == typeof(bool))
        {
            return new Dictionary<string, object> { ["type"] = "boolean" };
        }

        if (IsNumeric(type))
        {
            return new Dictionary<string, object>
            {
                ["type"] = type == typeof(int) || type == typeof(long) ? "integer" : "number"
            };
        }

        if (type.IsEnum)
        {
            var names = Enum.GetNames(type);
            var values = new List<object>();
            foreach (var n in names)
            {
                values.Add(n);
            }

            return new Dictionary<string, object>
            {
                ["type"] = "string",
                ["enum"] = values
            };
        }

        if (type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)) ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IList<>)) ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
        {
            var elementType = type.IsArray
                ? type.GetElementType()!
                : type.GetGenericArguments()[0];
            return new Dictionary<string, object>
            {
                ["type"] = "array",
                ["items"] = BuildTypeSchema(elementType)
            };
        }

        if (type == typeof(Dictionary<string, object>) ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>)))
        {
            return new Dictionary<string, object>
            {
                ["type"] = "object",
                ["additionalProperties"] = true
            };
        }

        // 复杂对象：展开公共属性
        if (type.IsClass && type != typeof(object))
        {
            var props = new Dictionary<string, object>();
            var req = new List<string>();
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var ps = BuildTypeSchema(prop.PropertyType);
                props[prop.Name] = ps;
                // 可空引用类型粗略判断：值类型可空不算必填
                var nt = Nullable.GetUnderlyingType(prop.PropertyType);
                if (nt == null && prop.PropertyType.IsValueType)
                {
                    req.Add(prop.Name);
                }
            }

            var objSchema = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = props
            };
            if (req.Count > 0)
            {
                objSchema["required"] = req;
            }

            return objSchema;
        }

        // 兜底
        return new Dictionary<string, object> { ["type"] = "string" };
    }

    private static bool IsNumeric(Type type)
    {
        return type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
               type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) || type == typeof(sbyte) ||
               type == typeof(double) || type == typeof(float) || type == typeof(decimal);
    }
}
