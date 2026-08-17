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
/// Determines whether model responses are cached and reused for identical requests.
/// Corresponds to Java: io.agentscope.core.model.CachePolicy
/// 缓存策略枚举，用于控制模型响应缓存行为。
/// 决定是否缓存模型响应并在相同请求时重用。
/// 对应 Java: io.agentscope.core.model.CachePolicy
/// </summary>
public enum CachePolicy
{
    /// <summary>
    /// Use the system default caching behavior (typically enabled).
    /// 使用系统默认的缓存行为（通常为启用）。
    /// </summary>
    Default = 0,

    /// <summary>
    /// Disable caching entirely. Each request will result in a new model call.
    /// 完全禁用缓存。每个请求都会触发新的模型调用。
    /// </summary>
    Disabled,

    /// <summary>
    /// Enable caching for model responses. Identical requests may return cached results.
    /// 启用以缓存模型响应。相同的请求可能返回缓存结果。
    /// </summary>
    Enabled
}
