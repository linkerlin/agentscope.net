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
/// JSON 编解码异常。对应 Java: io.agentscope.core.util.JsonException
/// </summary>
public class JsonException : System.Exception
{
    public JsonException(string message) : base(message) { }
    public JsonException(string message, System.Exception inner) : base(message, inner) { }
}

/// <summary>
/// JSON 编解码抽象接口，允许替换底层实现（System.Text.Json / Newtonsoft 等）。
/// 对应 Java: io.agentscope.core.util.JsonCodec
/// </summary>
public interface IJsonCodec
{
    string Encode(object? value);
    T? Decode<T>(string json);
    object? Decode(string json, System.Type type);
}

/// <summary>
/// 基于 System.Text.Json 的默认编解码实现。
/// 对应 Java: io.agentscope.core.util.JacksonJsonCodec
/// </summary>
public class SystemTextJsonCodec : IJsonCodec
{
    public static readonly SystemTextJsonCodec Instance = new();

    public string Encode(object? value)
    {
        try
        {
            return JsonSerializer.Serialize(value, JsonUtils.DefaultOptions);
        }
        catch (System.Exception ex)
        {
            throw new JsonException("JSON 编码失败", ex);
        }
    }

    public T? Decode<T>(string json)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return default;
            return JsonSerializer.Deserialize<T>(json, JsonUtils.DefaultOptions);
        }
        catch (System.Exception ex)
        {
            throw new JsonException("JSON 解码失败", ex);
        }
    }

    public object? Decode(string json, System.Type type)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize(json, type, JsonUtils.DefaultOptions);
        }
        catch (System.Exception ex)
        {
            throw new JsonException("JSON 解码失败", ex);
        }
    }
}
