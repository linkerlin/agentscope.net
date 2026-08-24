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

namespace AgentScope.Core.Message;

/// <summary>
/// Data block for carrying multimodal message content.
/// Can contain text and multiple media sources (images, audio, video, etc.).
/// Corresponds to Java: io.agentscope.core.message.DataBlock
/// 数据块，用于承载多模态消息内容。
/// 可包含文本和多个媒体来源（图片、音频、视频等）。
/// 对应 Java: io.agentscope.core.message.DataBlock
/// </summary>
public record DataBlock
{
    /// <summary>
    /// Text content of the data block.
    /// 数据块的文本内容。
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// List of media sources (images, audio, video, etc.).
    /// 媒体来源列表（图片、音频、视频等）。
    /// </summary>
    public List<Source>? Sources { get; set; }
}
