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

/// <summary>任务运行规格，对�?Java TaskRunSpec</summary>
public abstract record TaskRunSpec;

/// <summary>本地执行任务</summary>
public sealed record LocalTaskRunSpec(Func<Task<string?>> Execution) : TaskRunSpec;

/// <summary>远程 HTTP 任务</summary>
public sealed record RemoteTaskRunSpec(
    string BaseUrl,
    Dictionary<string, string>? Headers,
    string AgentId,
    string Input,
    RemoteSubmitContext? Context = null) : TaskRunSpec;

/// <summary>适配已有 Task 的任�?/summary>
public sealed record AdoptedTaskRunSpec(Task<string?> Future) : TaskRunSpec;

