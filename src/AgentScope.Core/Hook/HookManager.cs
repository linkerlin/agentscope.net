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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgentScope.Core.Hook;

/// <summary>
/// Hook 管理器
/// Hook manager for registering and executing hooks
/// </summary>
public class HookManager
{
    private readonly List<IHook> _hooks = new();

    public void RegisterHook(IHook hook)
    {
        _hooks.Add(hook);
    }

    public void UnregisterHook(IHook hook)
    {
        _hooks.Remove(hook);
    }

    public void ClearHooks()
    {
        _hooks.Clear();
    }

    public async Task ExecutePreReasoningHooksAsync(PreReasoningEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnPreReasoningAsync(@event);
            if (@event.ShouldStop) break;
        }
    }

    public async Task ExecutePostReasoningHooksAsync(PostReasoningEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnPostReasoningAsync(@event);
            if (@event.ShouldStop) break;
        }
    }

    public async Task ExecutePreActingHooksAsync(PreActingEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnPreActingAsync(@event);
            if (@event.ShouldStop) break;
        }
    }

    public async Task ExecutePostActingHooksAsync(PostActingEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnPostActingAsync(@event);
            if (@event.ShouldStop) break;
        }
    }

    public async Task ExecutePreSummaryHooksAsync(PreSummaryEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnPreSummaryAsync(@event);
            if (@event.ShouldStop) break;
        }
    }

    public async Task ExecutePostSummaryHooksAsync(PostSummaryEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnPostSummaryAsync(@event);
            if (@event.ShouldStop) break;
        }
    }

    public async Task ExecuteReasoningChunkHooksAsync(ReasoningChunkEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnReasoningChunkAsync(@event);
            if (@event.ShouldStop) break;
        }
    }

    public async Task ExecuteActingChunkHooksAsync(ActingChunkEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnActingChunkAsync(@event);
            if (@event.ShouldStop) break;
        }
    }

    public async Task ExecuteSummaryChunkHooksAsync(SummaryChunkEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnSummaryChunkAsync(@event);
            if (@event.ShouldStop) break;
        }
    }

    public async Task ExecuteErrorHooksAsync(ErrorHookEvent @event)
    {
        foreach (var hook in _hooks)
        {
            await hook.OnErrorAsync(@event);
            if (@event.ShouldStop) break;
        }
    }
}
