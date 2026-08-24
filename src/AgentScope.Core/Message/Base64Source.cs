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

namespace AgentScope.Core.Message;

/// <summary>
/// Base64-encoded media source, representing media content as a Base64 data string.
/// Corresponds to Java: io.agentscope.core.message.Base64Source
/// Base64 编码的媒体来源，表示由 Base64 编码数据表示的媒体内容。
/// 对应 Java: io.agentscope.core.message.Base64Source
/// </summary>
public record Base64Source : Source
{
    /// <summary>Source type identifier, always "base64" / 来源类型标识，固定为 "base64"。</summary>
    public override string Type => "base64";

    /// <summary>
    /// Media type (e.g., "image/png", "audio/mpeg", "video/mp4").
    /// 媒体类型（例如 "image/png"、"audio/mpeg"、"video/mp4"）。
    /// </summary>
    public required string MediaType { get; set; }

    /// <summary>
    /// Base64-encoded media data string.
    /// Base64 编码的媒体数据字符串。
    /// </summary>
    public required string Data { get; set; }
}
