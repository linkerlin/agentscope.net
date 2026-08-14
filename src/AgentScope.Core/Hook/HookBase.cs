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
/// Hook 基类，提供默认的空实现
/// Base hook class with default empty implementations
/// </summary>
public abstract class HookBase : IHook
{
    public virtual string Name => GetType().Name;

    public virtual Task OnPreReasoningAsync(PreReasoningEvent @event)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPostReasoningAsync(PostReasoningEvent @event)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPreActingAsync(PreActingEvent @event)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPostActingAsync(PostActingEvent @event)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPreSummaryAsync(PreSummaryEvent @event)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnPostSummaryAsync(PostSummaryEvent @event)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnReasoningChunkAsync(ReasoningChunkEvent @event)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnActingChunkAsync(ActingChunkEvent @event)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnSummaryChunkAsync(SummaryChunkEvent @event)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnErrorAsync(ErrorHookEvent @event)
    {
        return Task.CompletedTask;
    }
}
