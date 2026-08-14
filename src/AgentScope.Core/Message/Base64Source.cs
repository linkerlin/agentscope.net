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
/// Base64 编码的媒体来源，表示由 Base64 编码数据表示的媒体内容
/// </summary>
public record Base64Source : Source
{
    public override string Type => "base64";

    /// <summary>
    /// 媒体类型（如 "image/png", "audio/mpeg"）
    /// </summary>
    public required string MediaType { get; set; }

    /// <summary>
    /// Base64 编码的媒体数据
    /// </summary>
    public required string Data { get; set; }
}
