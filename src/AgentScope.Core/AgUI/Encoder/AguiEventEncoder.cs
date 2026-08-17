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
using AgentScope.Core.AgUI.Event;

namespace AgentScope.Core.AgUI.Encoder;

/// <summary>
/// AG-UI event SSE (Server-Sent Events) encoder, serializes <see cref="AguiEvent"/> to SSE format.
/// AG-UI 事件 SSE 编码器，将 AguiEvent 序列化为 SSE（Server-Sent Events）格式。
/// Corresponds to Java: AguiEventEncoder
/// </summary>
public static class AguiEventEncoder
{
    /// <summary>
    /// JSON serialization options: camelCase naming, no indentation.
    /// JSON 序列化选项：camelCase 命名，不缩进。
    /// </summary>
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Encodes an <see cref="AguiEvent"/> as an SSE data line.
    /// 将 AguiEvent 编码为 SSE data 行。
    /// </summary>
    /// <param name="evt">The event to encode / 要编码的事件</param>
    /// <returns>SSE-formatted string (e.g., "data: {...}\n\n") / SSE 格式字符串</returns>
    public static string Encode(AguiEvent evt)
    {
        var json = EncodeToJson(evt);
        return $"data: {json}\n\n";
    }

    /// <summary>
    /// Encodes an <see cref="AguiEvent"/> to a JSON string only.
    /// 仅将 AguiEvent 编码为 JSON 字符串。
    /// </summary>
    /// <param name="evt">The event to encode / 要编码的事件</param>
    /// <returns>JSON string / JSON 字符串</returns>
    public static string EncodeToJson(AguiEvent evt)
    {
        return JsonSerializer.Serialize(evt, JsonOpts);
    }

    /// <summary>
    /// Encodes a comment line in SSE format.
    /// 编码 SSE 注释行。
    /// </summary>
    /// <param name="comment">The comment text / 注释文本</param>
    /// <returns>SSE comment string / SSE 注释字符串</returns>
    public static string EncodeComment(string comment) => $": {comment}\n\n";

    /// <summary>
    /// Generates a keepalive signal for SSE connections.
    /// 生成 SSE 连接的保活信号。
    /// </summary>
    /// <returns>SSE keepalive string / SSE 保活字符串</returns>
    public static string KeepAlive() => ": keepalive\n\n";
}
