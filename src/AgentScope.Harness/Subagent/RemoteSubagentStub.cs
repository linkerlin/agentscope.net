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
using AgentScope.Core.Message;

namespace AgentScope.Harness.Subagent;

/// <summary>
/// Remote subagent stub. Placeholder for agents executed via HTTP transport.
/// 远程子代理存根。用于通过 HTTP 传输执行的 Agent 占位。
/// </summary>
public sealed class RemoteSubagentStub : AgentBase
{
    /// <summary>
    /// Initializes a new RemoteSubagentStub.
    /// 初始化远程子代理存根。
    /// </summary>
    /// <param name="name">Agent name / Agent 名称</param>
    /// <param name="description">Optional description / 可选描述</param>
    public RemoteSubagentStub(string name, string? description = null)
        : base(name, description ?? $"Remote subagent stub: {name}")
    {
    }

    /// <inheritdoc />
    protected override Task<Msg> DoCallAsync(IReadOnlyList<Msg> messages)
    {
        var msg = Msg.Builder()
            .Role("assistant")
            .Name(Name)
            .TextContent("[此子代理仅支持远程 HTTP 执行，请通过远程传输层调用]")
            .Build();
        return Task.FromResult(msg);
    }
}
