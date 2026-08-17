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
/// Team orchestration client. Implements optimistic concurrency via CAS (Compare-And-Swap).
/// 团队编排客户端。基于 CAS（Compare-And-Swap）实现乐观并发。
/// </summary>
public interface ITeamClient
{
    /// <summary>
    /// Creates a team task.
    /// 创建一个团队任务。
    /// </summary>
    /// <param name="task">The task to create / 待创建的任务</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>The created task ID / 创建的任务 ID</returns>
    Task<string> CreateTaskAsync(TeamTask task, CancellationToken ct = default);

    /// <summary>
    /// Claims a pending task for a member using CAS.
    /// 使用 CAS 认领一个待处理任务。
    /// </summary>
    /// <param name="taskId">Task ID / 任务 ID</param>
    /// <param name="memberId">Member ID / 成员 ID</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>True if claimed successfully / 成功认领返回 true</returns>
    Task<bool> ClaimTaskAsync(string taskId, string memberId, CancellationToken ct = default);

    /// <summary>
    /// Marks a task as completed.
    /// 将任务标记为完成。
    /// </summary>
    /// <param name="taskId">Task ID / 任务 ID</param>
    /// <param name="result">Task result / 任务结果</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    Task CompleteTaskAsync(string taskId, string result, CancellationToken ct = default);

    /// <summary>
    /// Marks a task as failed.
    /// 将任务标记为失败。
    /// </summary>
    /// <param name="taskId">Task ID / 任务 ID</param>
    /// <param name="error">Error description / 错误描述</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    Task FailTaskAsync(string taskId, string error, CancellationToken ct = default);

    /// <summary>
    /// Lists tasks, optionally filtered by member ID.
    /// 列出任务，可按成员 ID 过滤。
    /// </summary>
    /// <param name="memberId">Optional member ID filter / 可选的成员 ID 过滤</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>List of tasks / 任务列表</returns>
    Task<IReadOnlyList<TeamTask>> ListTasksAsync(string? memberId = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a message to a target member.
    /// 向目标成员发送消息。
    /// </summary>
    /// <param name="targetMember">Target member ID / 目标成员 ID</param>
    /// <param name="message">Message to send / 待发送的消息</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    ValueTask SendMessageAsync(string targetMember, TeamMessage message, CancellationToken ct = default);

    /// <summary>
    /// Reads all pending messages from an inbox.
    /// 从收件箱读取所有待处理消息。
    /// </summary>
    /// <param name="inbox">Inbox name / 收件箱名称</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>Async enumerable of messages / 消息的异步枚举</returns>
    IAsyncEnumerable<TeamMessage> ReadMessagesAsync(string inbox, CancellationToken ct = default);
}

/// <summary>
/// Represents a team task with version-based optimistic concurrency.
/// 表示一个具有版本乐观并发控制的团队任务。
/// </summary>
/// <param name="Id">Unique task ID / 唯一任务 ID</param>
/// <param name="Description">Task description / 任务描述</param>
/// <param name="AssignedTo">Member assigned to the task / 被分配该任务的成员</param>
/// <param name="Status">Task status (pending/in_progress/completed/failed) / 任务状态</param>
/// <param name="Result">Task result / 任务结果</param>
/// <param name="Version">Optimistic concurrency version / 乐观并发版本号</param>
public readonly record struct TeamTask(
    string Id, string Description, string AssignedTo = "",
    string Status = "pending", string Result = "", int Version = 1);

/// <summary>
/// Represents a message exchanged between team members.
/// 表示团队成员之间交换的消息。
/// </summary>
/// <param name="From">Sender member ID / 发送者成员 ID</param>
/// <param name="To">Recipient member ID / 接收者成员 ID</param>
/// <param name="Content">Message content / 消息内容</param>
/// <param name="SentAt">Timestamp when sent / 发送时间戳</param>
public readonly record struct TeamMessage(string From, string To, string Content, DateTime SentAt);
