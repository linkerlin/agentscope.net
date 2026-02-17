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
using AgentScope.Core.Message;

namespace AgentScope.Core.Hook;

/// <summary>
/// Hook 事件基类
/// Base class for hook events
/// </summary>
public abstract class HookEvent
{
    public string AgentName { get; set; } = "";
    public Msg? CurrentMessage { get; set; }
    public bool ShouldStop { get; set; } = false;
}

/// <summary>
/// 推理前事件
/// Pre-reasoning event
/// </summary>
public class PreReasoningEvent : HookEvent
{
    public string Context { get; set; } = "";
}

/// <summary>
/// 推理后事件
/// Post-reasoning event
/// </summary>
public class PostReasoningEvent : HookEvent
{
    public string ReasoningResult { get; set; } = "";
}

/// <summary>
/// 行动前事件
/// Pre-acting event
/// </summary>
public class PreActingEvent : HookEvent
{
    public string Action { get; set; } = "";
    public object? ActionParameters { get; set; }
}

/// <summary>
/// 行动后事件
/// Post-acting event
/// </summary>
public class PostActingEvent : HookEvent
{
    public string Action { get; set; } = "";
    public object? ActionResult { get; set; }
    public bool ActionSuccess { get; set; }
}

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
}

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
}

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
}
