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
/// Represents a wakeup notification sent to wake a team member for coordination.
/// 表示唤醒团队成员以进行协调的通知。
/// </summary>
/// <param name="AgentId">Target agent to wake / 目标代理 ID</param>
/// <param name="FromAgentId">Source agent sending the wakeup / 发送唤醒的源代理 ID</param>
/// <param name="Message">Optional wakeup message / 可选的唤醒消息</param>
/// <param name="SentAt">Timestamp when sent / 发送时间戳</param>
public sealed record TeamWakeup(string AgentId, string FromAgentId, string? Message = null, DateTime? SentAt = null);
