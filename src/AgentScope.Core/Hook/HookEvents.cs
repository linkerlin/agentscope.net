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
/// 摘要前事件
/// </summary>
public class PreSummaryEvent : HookEvent
{
    public string SummaryText { get; set; } = "";
}

/// <summary>
/// 摘要后事件
/// </summary>
public class PostSummaryEvent : HookEvent
{
    public string SummaryText { get; set; } = "";
}

/// <summary>
/// 推理块事件（流式推理过程中的单块内容）
/// </summary>
public class ReasoningChunkEvent : HookEvent
{
    public string Chunk { get; set; } = "";
}

/// <summary>
/// 行动块事件（流式行动过程中的单块内容）
/// </summary>
public class ActingChunkEvent : HookEvent
{
    public string Chunk { get; set; } = "";
}

/// <summary>
/// 摘要块事件（流式最终答复中的单块内容）
/// </summary>
public class SummaryChunkEvent : HookEvent
{
    public string Chunk { get; set; } = "";
}

/// <summary>
/// 错误事件
/// </summary>
public class ErrorHookEvent : HookEvent
{
    public string ErrorMessage { get; set; } = "";
    public System.Exception? Exception { get; set; }
}
