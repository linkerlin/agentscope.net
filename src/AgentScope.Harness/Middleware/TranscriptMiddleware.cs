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

using AgentScope.Harness.Transcript;

namespace AgentScope.Harness.Middleware;

/// <summary>
/// 会话转录中间件。对标 Java TranscriptMiddleware。
/// 将每次 agent 调用记录为转录分段。
/// </summary>
public sealed class TranscriptMiddleware(ITranscriptStore store) : IHarnessMiddleware
{
    public int Order => 900;

    public async ValueTask OnAgentAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default)
    {
        var sessionId = ctx.Items.GetValueOrDefault("session_id") as string ?? "default";
        await next();

        var segment = new TranscriptSegment(0, 1, "agent", $"[{DateTime.UtcNow:O}] {ctx.AgentName} 调用完成", DateTime.UtcNow);
        await store.AppendSegmentAsync(sessionId, segment, ct);
    }

    public ValueTask OnModelCallAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();

    public ValueTask OnToolExecutionAsync(MiddlewareContext ctx,
        Func<ValueTask> next, CancellationToken ct = default) => next();
}
