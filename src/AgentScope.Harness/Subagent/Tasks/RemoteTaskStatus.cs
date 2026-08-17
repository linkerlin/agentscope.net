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

using AgentScope.Harness.Subagent.Protocol;
namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>远程任务状态，对应 Java RemoteTaskStatus</summary>
public sealed record RemoteTaskStatus(
    string Status,
    string? Error = null,
    List<RemotePendingConfirm>? PendingConfirms = null)
{
    public bool IsAwaitingConfirm => Status == "awaiting_confirm";
    public bool IsTerminalSuccess => Status == "success";
    public bool IsTerminalFailure => Status is "error" or "failed";
    public bool IsCancelled => Status is "cancelled" or "canceled";
}

