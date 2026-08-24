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

namespace AgentScope.Harness.Team;

/// <summary>
/// Thrown when a CAS-based team operation encounters a version conflict.
/// 当基于 CAS 的团队操作遇到版本冲突时抛出。
/// </summary>
public sealed class TeamConflictException : Exception
{
    /// <summary>
    /// Creates a new TeamConflictException.
    /// 创建 TeamConflictException。
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    public TeamConflictException(string message) : base(message) { }

    /// <summary>
    /// Creates a new TeamConflictException with an inner exception.
    /// 创建包含内部异常的 TeamConflictException。
    /// </summary>
    /// <param name="message">Error message / 错误消息</param>
    /// <param name="inner">Inner exception / 内部异常</param>
    public TeamConflictException(string message, Exception inner) : base(message, inner) { }
}
