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
/// URL media source, representing media content referenced by a URL.
/// Corresponds to Java: io.agentscope.core.message.URLSource
/// URL 媒体来源，表示由 URL 指向的媒体内容。
/// 对应 Java: io.agentscope.core.message.URLSource
/// </summary>
public record URLSource : Source
{
    /// <summary>
    /// Gets the source type identifier, always "url".
    /// 获取来源类型标识，固定为 "url"。
    /// </summary>
    public override string Type => "url";

    /// <summary>
    /// Gets or sets the URL address of the media resource.
    /// 获取或设置媒体资源的 URL 地址。
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Gets or sets the optional MIME type of the media (e.g., "image/png", "audio/mp3").
    /// 获取或设置媒体的可选 MIME 类型（例如 "image/png"、"audio/mp3"）。
    /// </summary>
    public string? MimeType { get; set; }
}
