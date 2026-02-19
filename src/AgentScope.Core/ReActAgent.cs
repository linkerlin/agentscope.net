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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Memory;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Tool;

namespace AgentScope.Core;

/// <summary>
/// ReAct (Reasoning and Acting) Agent implementation.
/// ReAct Agent 实现 - 结合推理和行动的迭代循环
/// 
/// Features:
/// - Reasoning: Agent 分析当前情况，决定下一步行动
/// - Acting: 执行工具调用或返回最终答案
/// - Observation: 获取行动结果，继续循环或结束
/// 
/// ReAct Loop:
/// 1. Thought: Agent 思考下一步
/// 2. Action: 选择工具或返回答案
/// 3. Observation: 观察执行结果
/// 4. 重复直到完成或达到最大迭代次数
/// </summary>
public class ReActAgent : AgentBase
{
    private readonly IModel _model;
    private readonly IMemory _memory;
    private readonly Dictionary<string, ITool> _tools;
    private readonly string _systemPrompt;
    private readonly int _maxIterations;

    internal ReActAgent(string name, IModel model, string systemPrompt, 
                        IMemory? memory = null, List<ITool>? tools = null, 
                        int maxIterations = 10)
        : base(name)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _systemPrompt = systemPrompt ?? "You are a helpful AI assistant.";
        _memory = memory ?? new MemoryBase();
        _tools = tools?.ToDictionary(t => t.Name) ?? new Dictionary<string, ITool>();
        _maxIterations = maxIterations > 0 ? maxIterations : 10;
    }

    public override IObservable<Msg> Call(Msg message)
    {
        return Observable.FromAsync(async () =>
        {
            _memory.Add(message);

            // 如果有工具，执行完整的 ReAct 循环
            Msg response;
            if (_tools.Count > 0)
            {
                response = await ProcessWithReActLoopAsync(message);
            }
            else
            {
                // 没有工具时，简单调用模型
                response = await ProcessSimpleAsync(message);
            }
            
            _memory.Add(response);
            return response;
        });
    }

    /// <summary>
    /// 简单模式：无工具时的直接调用
    /// </summary>
    private async Task<Msg> ProcessSimpleAsync(Msg userMessage)
    {
        var messages = BuildMessageHistory();
        var request = new ModelRequest { Messages = messages };
        var response = await _model.GenerateAsync(request);

        if (!response.Success)
        {
            return Msg.Builder()
                .Name(Name)
                .Role("assistant")
                .TextContent($"Error: {response.Error}")
                .Build();
        }

        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent(response.Text ?? string.Empty)
            .Build();
    }

    /// <summary>
    /// ReAct 循环：推理-行动-观察
    /// </summary>
    private async Task<Msg> ProcessWithReActLoopAsync(Msg userMessage)
    {
        var iteration = 0;
        var continueLoop = true;
        var finalResponse = "";
        var thoughtHistory = new List<string>();

        while (continueLoop && iteration < _maxIterations)
        {
            iteration++;

            // 阶段 1: Reasoning（推理）
            var reasoningResult = await ReasoningPhaseAsync(userMessage, thoughtHistory, iteration);
            
            if (reasoningResult.IsError)
            {
                return CreateErrorResponse(reasoningResult.ErrorMessage!);
            }

            thoughtHistory.Add($"Thought {iteration}: {reasoningResult.Thought}");

            // 阶段 2: Acting（行动）
            var actionResult = await ActingPhaseAsync(reasoningResult);
            
            if (actionResult.IsFinish)
            {
                finalResponse = actionResult.FinalAnswer!;
                continueLoop = false;
            }
            else if (actionResult.IsToolCall)
            {
                // 阶段 3: Observation（观察）
                var observation = $"Tool '{actionResult.ToolName}' result: {actionResult.ToolResult}";
                thoughtHistory.Add($"Observation {iteration}: {observation}");
            }
            else if (actionResult.IsError)
            {
                return CreateErrorResponse(actionResult.ErrorMessage!);
            }
        }

        if (iteration >= _maxIterations && string.IsNullOrEmpty(finalResponse))
        {
            finalResponse = "达到最大迭代次数但未得出结论。";
        }

        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent(finalResponse)
            .AddMetadata("iterations", iteration)
            .AddMetadata("thoughts", string.Join("\n", thoughtHistory))
            .Build();
    }

    /// <summary>
    /// 推理阶段：Agent 思考下一步该做什么
    /// </summary>
    private async Task<ReasoningResult> ReasoningPhaseAsync(
        Msg userMessage, 
        List<string> thoughtHistory, 
        int iteration)
    {
        try
        {
            var prompt = BuildReActPrompt(userMessage, thoughtHistory, iteration);
            var request = new ModelRequest { Messages = new List<Msg> { prompt } };
            var response = await _model.GenerateAsync(request);

            if (!response.Success)
            {
                return ReasoningResult.Error(response.Error ?? "模型错误");
            }

            var thought = ParseThought(response.Text ?? "");
            return ReasoningResult.Success(thought);
        }
        catch (System.Exception ex)
        {
            return ReasoningResult.Error($"推理错误：{ex.Message}");
        }
    }

    /// <summary>
    /// 行动阶段：根据推理结果执行行动
    /// </summary>
    private async Task<ActionResult> ActingPhaseAsync(ReasoningResult reasoning)
    {
        try
        {
            var intent = ParseActionIntent(reasoning.Thought!);

            if (intent.Action == "finish")
            {
                return ActionResult.Finish(intent.Parameters?.ToString() ?? "Done");
            }
            else if (_tools.TryGetValue(intent.Action, out var tool))
            {
                var parameters = intent.Parameters as Dictionary<string, object> 
                    ?? new Dictionary<string, object>();
                var toolResult = await tool.ExecuteAsync(parameters);
                
                return ActionResult.ToolCall(
                    intent.Action, 
                    toolResult.Success, 
                    toolResult.Result?.ToString() ?? toolResult.Error ?? "");
            }
            else
            {
                // 未知行动，当作完成处理
                return ActionResult.Finish(reasoning.Thought ?? "Done");
            }
        }
        catch (System.Exception ex)
        {
            return ActionResult.Error($"行动错误：{ex.Message}");
        }
    }

    private Msg BuildReActPrompt(Msg userMessage, List<string> thoughtHistory, int iteration)
    {
        var toolDescriptions = string.Join("\n", 
            _tools.Values.Select(t => $"- {t.Name}: {t.Description}"));

        var promptText = $@"{_systemPrompt}

User Question: {userMessage.GetTextContent()}

Available Tools:
{toolDescriptions}

Previous Thoughts:
{string.Join("\n", thoughtHistory)}

Current Iteration: {iteration}/{_maxIterations}

Respond in this format:
Thought: [Your reasoning process]
Action: [finish or tool name]
Action Input: [Final answer if finish, or JSON parameters if tool]";

        return Msg.Builder()
            .Role("user")
            .TextContent(promptText)
            .Build();
    }

    private List<Msg> BuildMessageHistory()
    {
        var messages = new List<Msg>();
        
        if (!string.IsNullOrEmpty(_systemPrompt))
        {
            messages.Add(Msg.Builder()
                .Role("system")
                .TextContent(_systemPrompt)
                .Build());
        }

        messages.AddRange(_memory.GetAll());
        return messages;
    }

    private string ParseThought(string response)
    {
        var lines = response.Split('\n');
        var thoughtLine = lines.FirstOrDefault(l => 
            l.StartsWith("Thought:", StringComparison.OrdinalIgnoreCase));
        return thoughtLine?.Substring("Thought:".Length).Trim() ?? response;
    }

    private ActionIntent ParseActionIntent(string thought)
    {
        try
        {
            var lines = thought.Split('\n');
            var actionLine = lines.FirstOrDefault(l => 
                l.StartsWith("Action:", StringComparison.OrdinalIgnoreCase));
            var inputLine = lines.FirstOrDefault(l => 
                l.StartsWith("Action Input:", StringComparison.OrdinalIgnoreCase));

            var action = actionLine?.Substring("Action:".Length).Trim().ToLower() ?? "finish";
            var input = inputLine?.Substring("Action Input:".Length).Trim() ?? "";

            object? parameters = null;
            if (!string.IsNullOrEmpty(input))
            {
                try
                {
                    parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(input);
                }
                catch
                {
                    parameters = input;
                }
            }

            return new ActionIntent { Action = action, Parameters = parameters };
        }
        catch
        {
            return new ActionIntent { Action = "finish", Parameters = thought };
        }
    }

    private Msg CreateErrorResponse(string error)
    {
        return Msg.Builder()
            .Name(Name)
            .Role("assistant")
            .TextContent($"错误：{error}")
            .Build();
    }

    public static ReActAgentBuilder Builder()
    {
        return new ReActAgentBuilder();
    }
}

/// <summary>
/// ReActAgent 构建器
/// </summary>
public class ReActAgentBuilder
{
    private string _name = "ReActAgent";
    private IModel? _model;
    private string _sysPrompt = "You are a helpful AI assistant.";
    private IMemory? _memory;
    private readonly List<ITool> _tools = new();
    private int _maxIterations = 10;

    public ReActAgentBuilder Name(string name)
    {
        _name = name;
        return this;
    }

    public ReActAgentBuilder Model(IModel model)
    {
        _model = model;
        return this;
    }

    public ReActAgentBuilder SysPrompt(string prompt)
    {
        _sysPrompt = prompt;
        return this;
    }

    public ReActAgentBuilder Memory(IMemory memory)
    {
        _memory = memory;
        return this;
    }

    public ReActAgentBuilder AddTool(ITool tool)
    {
        _tools.Add(tool);
        return this;
    }

    public ReActAgentBuilder Tools(IEnumerable<ITool> tools)
    {
        _tools.AddRange(tools);
        return this;
    }

    public ReActAgentBuilder MaxIterations(int max)
    {
        _maxIterations = max;
        return this;
    }

    public ReActAgent Build()
    {
        if (_model == null)
        {
            throw new InvalidOperationException("必须指定模型");
        }

        return new ReActAgent(_name, _model, _sysPrompt, _memory, _tools, _maxIterations);
    }
}
