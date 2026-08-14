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

using System.Threading.Tasks;

namespace AgentScope.Core.Hook;

/// <summary>
/// Hook 接口
/// Hook interface for extensibility
/// </summary>
public interface IHook
{
    string Name { get; }

    Task OnPreReasoningAsync(PreReasoningEvent @event);

    Task OnPostReasoningAsync(PostReasoningEvent @event);

    Task OnPreActingAsync(PreActingEvent @event);

    Task OnPostActingAsync(PostActingEvent @event);

    Task OnPreSummaryAsync(PreSummaryEvent @event);

    Task OnPostSummaryAsync(PostSummaryEvent @event);

    /// <summary>推理块（流式）</summary>
    Task OnReasoningChunkAsync(ReasoningChunkEvent @event);

    /// <summary>行动块（流式）</summary>
    Task OnActingChunkAsync(ActingChunkEvent @event);

    /// <summary>摘要块（流式最终答复）</summary>
    Task OnSummaryChunkAsync(SummaryChunkEvent @event);

    /// <summary>错误</summary>
    Task OnErrorAsync(ErrorHookEvent @event);
}
