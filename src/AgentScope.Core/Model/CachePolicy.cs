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

namespace AgentScope.Core.Model;

/// <summary>
/// Cache policy enum for controlling model response caching behavior.
/// 缓存策略枚举，用于控制模型响应缓存行为。
/// </summary>
public enum CachePolicy
{
    /// <summary>
    /// Use the system default caching behavior.
    /// 使用系统默认的缓存行为。
    /// </summary>
    Default = 0,

    /// <summary>
    /// Disable caching entirely.
    /// 完全禁用缓存。
    /// </summary>
    Disabled,

    /// <summary>
    /// Enable caching for model responses.
    /// 启用以缓存模型响应。
    /// </summary>
    Enabled
}
