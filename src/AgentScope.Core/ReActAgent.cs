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
using System.Reactive.Linq;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Memory;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Tool;

namespace AgentScope.Core;

/// <summary>
/// ReAct (Reasoning and Acting) Agent implementation.
/// Combines reasoning (thinking and planning) with acting (tool execution) in an iterative loop.
/// </summary>
public class ReActAgent : AgentBase
{
    private readonly IModel _model;
    private readonly IMemory _memory;
    private readonly List<ITool> _tools;
    private readonly string _systemPrompt;
    private readonly int _maxIterations;

    internal ReActAgent(string name, IModel model, string systemPrompt, 
                        IMemory? memory = null, List<ITool>? tools = null, 
                        int maxIterations = 10)
        : base(name)
    {
        _model = model;
        _systemPrompt = systemPrompt;
        _memory = memory ?? new MemoryBase();
        _tools = tools ?? new List<ITool>();
        _maxIterations = maxIterations;
    }

    public override IObservable<Msg> Call(Msg message)
    {
        return Observable.FromAsync(async () =>
        {
            _memory.Add(message);

            var response = await ProcessAsync(message);
            
            _memory.Add(response);
            
            return response;
        });
    }

    private async Task<Msg> ProcessAsync(Msg userMessage)
    {
        var messages = BuildMessageHistory();
        
        var request = new 模型请求
        {
            Messages = messages
        };

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

    public static ReActAgentBuilder Builder()
    {
        return new ReActAgentBuilder();
    }
}

/// <summary>
/// Builder for ReActAgent
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

    public ReActAgentBuilder MaxIterations(int max)
    {
        _maxIterations = max;
        return this;
    }

    public ReActAgent Build()
    {
        if (_model == null)
        {
            throw new InvalidOperationException("Model is required");
        }

        return new ReActAgent(_name, _model, _sysPrompt, _memory, _tools, _maxIterations);
    }
}
