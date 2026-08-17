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
/// Abstract base class for polymorphic media sources (e.g., images, audio, video).
/// 多态媒体来源抽象基类，表示图片、音频、视频等媒体的来源。
/// </summary>
public abstract record Source
{
    /// <summary>
    /// Gets the type identifier of this source.
    /// 获取当前来源的类型标识符。
    /// </summary>
    public abstract string Type { get; }
}
