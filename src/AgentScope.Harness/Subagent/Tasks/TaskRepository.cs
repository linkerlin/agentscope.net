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

using AgentScope.Core.Agent;
namespace AgentScope.Harness.Subagent.Tasks;

/// <summary>任务仓库接口，对�?Java TaskRepository</summary>
public interface ITaskRepository
{
    BackgroundTask? GetTask(RuntimeContext? rc, string sessionId, string taskId);
    BackgroundTask PutTask(RuntimeContext? rc, string taskId, string subAgentId,
        string sessionId, TaskRunSpec spec);
    ICollection<BackgroundTask> ListTasks(RuntimeContext? rc,
        string sessionId, TaskStatus? filter = null);
    bool CancelTask(RuntimeContext? rc, string sessionId, string taskId);
}

