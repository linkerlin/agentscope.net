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

namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>任务记录，对�?Java TaskRecord</summary>
public sealed class TaskRecord
{
    public string TaskId { get; set; }
    public string SubAgentId { get; set; }
    public string ParentAgentId { get; set; } = "";
    public string ParentSessionId { get; set; } = "";
    public string SubSessionId { get; set; } = "";
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public string? Result { get; set; }
    public string? ErrorMessage { get; set; }
    public bool CancelRequested { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? TransportType { get; set; }
    public string? RemoteBaseUrl { get; set; }
    public bool AwaitingConfirm { get; set; }
    public long LastEventSeq { get; set; }

    public TaskRecord(string taskId, string subAgentId)
    {
        TaskId = taskId;
        SubAgentId = subAgentId;
    }

    public void Touch() => LastUpdatedAt = DateTime.UtcNow;
}

