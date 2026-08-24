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

using System.Text.Json.Serialization;

namespace AgentScope.Harness.Subagent.Protocol;

/// <summary>远程待确认项，对应 Java RemotePendingConfirm</summary>
public sealed class RemotePendingConfirm
{
    public string? ToolCallId { get; set; }
    public string? ToolName { get; set; }
    public string? ToolInputJson { get; set; }

    public RemotePendingConfirm() { }

    public RemotePendingConfirm(string toolCallId, string toolName, string toolInputJson)
    {
        ToolCallId = toolCallId;
        ToolName = toolName;
        ToolInputJson = toolInputJson;
    }
}
