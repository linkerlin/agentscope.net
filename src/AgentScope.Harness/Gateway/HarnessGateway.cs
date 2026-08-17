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

using System.Runtime.CompilerServices;
using AgentScope.Core.Agent;
using AgentScope.Core.Events;
using AgentScope.Core.Message;

namespace AgentScope.Harness.Gateway;

/// <summary>
/// Harness 网关实现。对标 Java HarnessGateway。
/// 包装 IAgent 提供流式与非流式入口。
/// </summary>
public sealed class HarnessGateway(IAgent agent) : IGateway
{
    /// <inheritdoc />
    public async Task<Msg> RunAsync(Msg input, RuntimeContext? context = null, CancellationToken ct = default)
    {
        return await agent.CallAsync(input, context);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Event> RunStreamAsync(Msg input,
        RuntimeContext? context = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var evt in agent.StreamEventsAsync(input, context))
        {
            ct.ThrowIfCancellationRequested();
            yield return evt;
        }
    }
}
