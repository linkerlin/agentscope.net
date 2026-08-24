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
using System.Text.Json;

namespace AgentScope.Core.Util;

/// <summary>
/// 类型工具类：JSON 值与 .NET 类型之间的安全转换。
/// 对应 Java: io.agentscope.core.util.TypeUtils
/// </summary>
public static class TypeUtils
{
    /// <summary>把 JSON 反序列化后得到的（可能为 JsonElement）值转换为目标类型。</summary>
    public static object? ConvertValue(object? value, System.Type targetType)
    {
        if (value == null) return targetType.IsValueType ? System.Activator.CreateInstance(targetType) : null;

        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (type == typeof(string)) return value.ToString();
        if (type == typeof(int)) return System.Convert.ToInt32(value);
        if (type == typeof(long)) return System.Convert.ToInt64(value);
        if (type == typeof(double)) return System.Convert.ToDouble(value);
        if (type == typeof(float)) return System.Convert.ToSingle(value);
        if (type == typeof(decimal)) return System.Convert.ToDecimal(value);
        if (type == typeof(bool)) return System.Convert.ToBoolean(value);
        if (type == typeof(System.DateTime)) return System.Convert.ToDateTime(value);

        if (value is System.Text.Json.JsonElement je)
        {
            return je.Deserialize(type, JsonUtils.DefaultOptions);
        }

        return System.Convert.ChangeType(value, type);
    }

    /// <summary>泛型转换便捷方法。</summary>
    public static T? Convert<T>(object? value) => (T?)ConvertValue(value, typeof(T));

    /// <summary>把对象转为字典（便于作为工具参数）。</summary>
    public static Dictionary<string, object> ToDictionary(object obj)
    {
        var json = JsonUtils.ToJson(obj);
        var dict = JsonUtils.FromJson<Dictionary<string, object>>(json);
        return dict ?? new Dictionary<string, object>();
    }
}
