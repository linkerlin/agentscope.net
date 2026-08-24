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

using System.Text.Json;

namespace AgentScope.Core.Util;

/// <summary>
/// JSON 工具类：统一 System.Text.Json 的序列化/反序列化选项与便捷方法。
/// 对应 Java: io.agentscope.core.util.JsonUtils
/// </summary>
public static class JsonUtils
{
    /// <summary>框架默认 JSON 选项（驼峰、忽略 null、不区分大小写）。</summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    /// <summary>带缩进的“可读”选项。</summary>
    public static readonly JsonSerializerOptions PrettyOptions = new(DefaultOptions)
    {
        WriteIndented = true
    };

    /// <summary>序列化为 JSON 字符串。</summary>
    public static string ToJson(object? value, bool indented = false)
    {
        return JsonSerializer.Serialize(value, indented ? PrettyOptions : DefaultOptions);
    }

    /// <summary>反序列化 JSON 字符串。</summary>
    public static T? FromJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return default;
        return JsonSerializer.Deserialize<T>(json, DefaultOptions);
    }

    /// <summary>反序列化为指定类型（运行时类型）。</summary>
    public static object? FromJson(string json, System.Type type)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize(json, type, DefaultOptions);
    }

    /// <summary>深拷贝：经 JSON 往返。</summary>
    public static T? DeepClone<T>(T value)
    {
        if (value == null) return default;
        return FromJson<T>(ToJson(value));
    }
}
