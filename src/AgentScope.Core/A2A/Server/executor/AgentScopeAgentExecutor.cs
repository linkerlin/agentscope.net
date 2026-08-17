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

using AgentScope.Core.A2A.Server.Executor.Runner;
using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Core.A2A.Server.Executor;

/// <summary>
/// A2A Agent 执行器。对标 Java AgentScopeAgentExecutor。
/// 支持阻塞模式（完整结果）和流式模式（逐个事件发送）。
/// </summary>
public sealed class AgentScopeAgentExecutor(IAgentRunner runner)
{
    public async Task<Msg> ExecuteAsync(IReadOnlyList<Msg> messages, AgentRequestOptions? options = null,
        CancellationToken ct = default)
    {
        Msg? result = null;
        await foreach (var evt in runner.StreamAsync(messages, options ?? new AgentRequestOptions(), ct))
        {
            if (evt.IsLast && evt.Message != null)
                result = evt.Message;
        }
        return result ?? Msg.Builder().Role("assistant").TextContent("").Build();
    }

    public IAsyncEnumerable<Event> StreamAsync(IReadOnlyList<Msg> messages, AgentRequestOptions? options = null,
        CancellationToken ct = default) =>
        runner.StreamAsync(messages, options ?? new AgentRequestOptions(), ct);
}
