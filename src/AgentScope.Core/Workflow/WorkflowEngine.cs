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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Tool;

namespace AgentScope.Core.Workflow;

/// <summary>
/// Workflow engine implementation with DAG support.
/// 工作流引擎实现（支持DAG）
/// 
/// Java参考: io.agentscope.core.workflow.WorkflowEngine
/// </summary>
public class WorkflowEngine : IWorkflowEngine
{
    private readonly IServiceProvider? _serviceProvider;
    private readonly Dictionary<string, IAgent> _agents;
    private readonly Dictionary<string, ITool> _tools;
    private readonly ConcurrentDictionary<string, WorkflowExecution> _executions = new();
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    public WorkflowEngine(
        Dictionary<string, IAgent>? agents = null,
        Dictionary<string, ITool>? tools = null,
        IServiceProvider? serviceProvider = null)
    {
        _agents = agents ?? new Dictionary<string, IAgent>();
        _tools = tools ?? new Dictionary<string, ITool>();
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Task<WorkflowResult> ExecuteAsync(WorkflowDefinition workflow, Dictionary<string, object>? inputs = null, CancellationToken cancellationToken = default)
    {
        var context = new WorkflowContext
        {
            Workflow = workflow,
            Inputs = inputs ?? new Dictionary<string, object>(),
            Agents = _agents,
            CancellationToken = cancellationToken
        };

        var execution = new WorkflowExecution(context, this);
        _executions[context.ExecutionId] = execution;

        return execution.ExecuteAsync();
    }

    /// <inheritdoc />
    public ValidationResult Validate(WorkflowDefinition workflow)
    {
        return workflow.Validate();
    }

    /// <inheritdoc />
    public Task<WorkflowExecutionStatus> GetStatusAsync(string executionId)
    {
        if (_executions.TryGetValue(executionId, out var execution))
        {
            return Task.FromResult(execution.Status);
        }
        return Task.FromResult(WorkflowExecutionStatus.Pending);
    }

    /// <inheritdoc />
    public async Task<bool> CancelAsync(string executionId)
    {
        if (_executions.TryGetValue(executionId, out var execution))
        {
            await execution.CancelAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Registers an agent.
    /// </summary>
    public void RegisterAgent(string name, IAgent agent)
    {
        _agents[name] = agent;
    }

    /// <summary>
    /// Registers a tool.
    /// </summary>
    public void RegisterTool(string name, ITool tool)
    {
        _tools[name] = tool;
    }

    /// <summary>
    /// Gets an agent by name.
    /// </summary>
    public IAgent? GetAgent(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _agents.TryGetValue(name, out var agent) ? agent : null;
    }

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    public ITool? GetTool(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _tools.TryGetValue(name, out var tool) ? tool : null;
    }

    /// <summary>
    /// Workflow execution instance.
    /// </summary>
    private class WorkflowExecution
    {
        private readonly WorkflowContext _context;
        private readonly WorkflowEngine _engine;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<WorkflowNodeResult>> _nodeCompletions = new();

        public WorkflowExecutionStatus Status { get; private set; } = WorkflowExecutionStatus.Pending;

        public WorkflowExecution(WorkflowContext context, WorkflowEngine engine)
        {
            _context = context;
            _engine = engine;
        }

        public async Task<WorkflowResult> ExecuteAsync()
        {
            Status = WorkflowExecutionStatus.Running;
            var startTime = DateTime.UtcNow;

            try
            {
                // Build connections
                _context.Workflow.BuildConnections();

                // Validate
                var validation = _context.Workflow.Validate();
                if (!validation.IsValid)
                {
                    return new WorkflowResult
                    {
                        WorkflowId = _context.Workflow.Id,
                        ExecutionId = _context.ExecutionId,
                        Status = WorkflowExecutionStatus.Failed,
                        Error = string.Join("; ", validation.Errors),
                        StartedAt = startTime,
                        CompletedAt = DateTime.UtcNow
                    };
                }

                // Initialize inputs
                foreach (var input in _context.Workflow.Inputs)
                {
                    if (_context.Inputs.TryGetValue(input.Name, out var value))
                    {
                        _context.SetValue(input.Name, value);
                    }
                    else if (input.DefaultValue != null)
                    {
                        _context.SetValue(input.Name, input.DefaultValue);
                    }
                }

                // Execute DAG
                await ExecuteDagAsync();

                // Collect outputs
                var outputs = new Dictionary<string, object>();
                foreach (var outputDef in _context.Workflow.Outputs)
                {
                    if (!string.IsNullOrEmpty(outputDef.Source))
                    {
                        var value = ResolveValue(outputDef.Source);
                        if (value != null)
                        {
                            outputs[outputDef.Name] = value;
                        }
                    }
                }

                Status = WorkflowExecutionStatus.Completed;

                return new WorkflowResult
                {
                    WorkflowId = _context.Workflow.Id,
                    ExecutionId = _context.ExecutionId,
                    Status = Status,
                    Outputs = outputs,
                    NodeResults = _context.Results.Values.ToList(),
                    StartedAt = startTime,
                    CompletedAt = DateTime.UtcNow
                };
            }
            catch (OperationCanceledException)
            {
                Status = WorkflowExecutionStatus.Cancelled;
                return new WorkflowResult
                {
                    WorkflowId = _context.Workflow.Id,
                    ExecutionId = _context.ExecutionId,
                    Status = Status,
                    NodeResults = _context.Results.Values.ToList(),
                    StartedAt = startTime,
                    CompletedAt = DateTime.UtcNow
                };
            }
            catch (System.Exception ex)
            {
                Status = WorkflowExecutionStatus.Failed;
                return new WorkflowResult
                {
                    WorkflowId = _context.Workflow.Id,
                    ExecutionId = _context.ExecutionId,
                    Status = Status,
                    Error = ex.Message,
                    NodeResults = _context.Results.Values.ToList(),
                    StartedAt = startTime,
                    CompletedAt = DateTime.UtcNow
                };
            }
        }

        private async Task ExecuteDagAsync()
        {
            var startNode = _context.Workflow.GetStartNode();
            if (startNode == null) return;

            var completedNodes = new ConcurrentDictionary<string, bool>();
            var runningNodes = new ConcurrentDictionary<string, int>();
            var executionQueue = new ConcurrentQueue<WorkflowNode>();
            var semaphore = new SemaphoreSlim(5, 5); // Limit parallelism

            // Queue start node
            executionQueue.Enqueue(startNode);

            async Task ProcessNode(WorkflowNode node)
            {
                try
                {
                    // Check cancellation
                    if (_cts.Token.IsCancellationRequested) return;

                    // Skip if already completed or running
                    if (completedNodes.ContainsKey(node.Id)) return;
                    
                    // Mark as running
                    runningNodes[node.Id] = 1;

                    // Execute the node
                    var result = await ExecuteNodeAsync(node);
                    _context.Results[node.Id] = result;

                    // Mark as completed
                    completedNodes[node.Id] = true;
                    runningNodes.TryRemove(node.Id, out _);

                    // Notify completion
                    if (_nodeCompletions.TryGetValue(node.Id, out var tcs))
                    {
                        tcs.TrySetResult(result);
                    }

                    // Queue downstream nodes if successful
                    if (result.Status == WorkflowNodeStatus.Completed)
                    {
                        foreach (var downstreamId in node.Downstream)
                        {
                            var downstream = _context.Workflow.GetNode(downstreamId);
                            if (downstream != null && !completedNodes.ContainsKey(downstreamId))
                            {
                                // Check if all dependencies are satisfied
                                var depsCompleted = downstream.Dependencies.All(d => completedNodes.ContainsKey(d));
                                if (depsCompleted)
                                {
                                    executionQueue.Enqueue(downstream);
                                }
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    runningNodes.TryRemove(node.Id, out _);
                    throw;
                }
            }

            // Process queue
            var tasks = new List<Task>();
            while (!executionQueue.IsEmpty || runningNodes.Count > 0)
            {
                if (_cts.Token.IsCancellationRequested) break;

                // Start new tasks up to the limit
                while (tasks.Count < 5 && executionQueue.TryDequeue(out var node))
                {
                    if (!completedNodes.ContainsKey(node.Id))
                    {
                        await semaphore.WaitAsync(_cts.Token);
                        var task = ProcessNode(node).ContinueWith(_ => semaphore.Release(), _cts.Token);
                        tasks.Add(task);
                    }
                }

                // Remove completed tasks
                tasks.RemoveAll(t => t.IsCompleted);

                // If no tasks running and queue is empty, we're done
                if (tasks.Count == 0 && executionQueue.IsEmpty)
                    break;

                // Small delay to prevent CPU spinning
                if (tasks.Count > 0)
                {
                    await Task.WhenAny(tasks).WaitAsync(TimeSpan.FromSeconds(30), _cts.Token);
                }
                else
                {
                    await Task.Delay(10, _cts.Token);
                }
            }

            // Wait for remaining tasks
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(30), _cts.Token);
            }
        }

        private async Task<WorkflowNodeResult> ExecuteNodeAsync(WorkflowNode node)
        {
            var startTime = DateTime.UtcNow;
            var result = new WorkflowNodeResult
            {
                NodeId = node.Id,
                Status = WorkflowNodeStatus.Running,
                StartedAt = startTime
            };

            try
            {
                // Resolve inputs
                var inputs = ResolveInputs(node.Inputs);

                // Execute based on node type
                switch (node.Type)
                {
                    case WorkflowNodeType.Start:
                        // Start node just passes through
                        result.Status = WorkflowNodeStatus.Completed;
                        break;

                    case WorkflowNodeType.End:
                        // End node marks completion
                        result.Status = WorkflowNodeStatus.Completed;
                        break;

                    case WorkflowNodeType.Task:
                        result = await ExecuteTaskNodeAsync(node, inputs);
                        break;

                    case WorkflowNodeType.Decision:
                        result = await ExecuteDecisionNodeAsync(node, inputs);
                        break;

                    case WorkflowNodeType.Parallel:
                        result = await ExecuteParallelNodeAsync(node, inputs);
                        break;

                    case WorkflowNodeType.Map:
                        result = await ExecuteMapNodeAsync(node, inputs);
                        break;

                    default:
                        result.Status = WorkflowNodeStatus.Failed;
                        result.Error = $"Unknown node type: {node.Type}";
                        break;
                }

                // Store outputs in context
                foreach (var output in result.Outputs)
                {
                    _context.SetValue($"{node.Id}.{output.Key}", output.Value);
                }

                result.CompletedAt = DateTime.UtcNow;
                return result;
            }
            catch (System.Exception ex)
            {
                result.Status = WorkflowNodeStatus.Failed;
                result.Error = ex.Message;
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }
        }

        private async Task<WorkflowNodeResult> ExecuteTaskNodeAsync(WorkflowNode node, Dictionary<string, object> inputs)
        {
            var result = new WorkflowNodeResult { NodeId = node.Id };

            // Try tool first
            if (!string.IsNullOrEmpty(node.ToolName))
            {
                var tool = _engine.GetTool(node.ToolName);
                if (tool != null)
                {
                    var toolResult = await tool.ExecuteAsync(inputs);
                    result.Status = toolResult.Success ? WorkflowNodeStatus.Completed : WorkflowNodeStatus.Failed;
                    result.Outputs["result"] = toolResult.Result ?? new Dictionary<string, object>();
                    if (!toolResult.Success)
                    {
                        result.Error = toolResult.Error;
                    }
                    return result;
                }
            }

            // Try agent
            if (!string.IsNullOrEmpty(node.AgentName))
            {
                var agent = _engine.GetAgent(node.AgentName);
                if (agent != null)
                {
                    var message = Msg.Builder()
                        .Role("user")
                        .TextContent(inputs.TryGetValue("prompt", out var prompt) ? prompt.ToString() : node.Description ?? "Execute task")
                        .Build();

                    var response = await agent.CallAsync(message);
                    result.Status = WorkflowNodeStatus.Completed;
                    result.Outputs["result"] = response.GetTextContent() ?? "";
                    return result;
                }
            }

            // No executor found
            result.Status = WorkflowNodeStatus.Failed;
            result.Error = "No agent or tool configured for this task";
            return result;
        }

        private Task<WorkflowNodeResult> ExecuteDecisionNodeAsync(WorkflowNode node, Dictionary<string, object> inputs)
        {
            var result = new WorkflowNodeResult { NodeId = node.Id };

            // Evaluate condition
            bool conditionMet = EvaluateCondition(node.Condition, inputs);

            result.Status = WorkflowNodeStatus.Completed;
            result.Outputs["condition_met"] = conditionMet;
            result.Outputs["selected_branch"] = conditionMet ? node.TrueBranch : node.FalseBranch;

            return Task.FromResult(result);
        }

        private async Task<WorkflowNodeResult> ExecuteParallelNodeAsync(WorkflowNode node, Dictionary<string, object> inputs)
        {
            var result = new WorkflowNodeResult { NodeId = node.Id };
            var childResults = new ConcurrentBag<WorkflowNodeResult>();

            var tasks = node.Children.Select(async child =>
            {
                var childResult = await ExecuteNodeAsync(child);
                childResults.Add(childResult);
            }).ToArray();

            await Task.WhenAll(tasks);

            var allSucceeded = childResults.All(r => r.Status == WorkflowNodeStatus.Completed);
            result.Status = allSucceeded ? WorkflowNodeStatus.Completed : WorkflowNodeStatus.Failed;
            result.Outputs["results"] = childResults.ToList();

            return result;
        }

        private async Task<WorkflowNodeResult> ExecuteMapNodeAsync(WorkflowNode node, Dictionary<string, object> inputs)
        {
            var result = new WorkflowNodeResult { NodeId = node.Id };

            if (!inputs.TryGetValue("items", out var items) || items is not System.Collections.IEnumerable enumerable)
            {
                result.Status = WorkflowNodeStatus.Failed;
                result.Error = "Map node requires 'items' input";
                return result;
            }

            var childResults = new ConcurrentBag<WorkflowNodeResult>();
            var itemsList = enumerable.Cast<object>().ToList();

            var tasks = itemsList.Select(async (item, index) =>
            {
                var childInputs = new Dictionary<string, object>(inputs)
                {
                    ["item"] = item,
                    ["index"] = index
                };

                var child = node.Children.FirstOrDefault();
                if (child != null)
                {
                    // Create a copy of child with resolved inputs
                    var childResult = await ExecuteNodeAsync(child);
                    childResults.Add(childResult);
                }
            }).ToArray();

            await Task.WhenAll(tasks);

            result.Status = WorkflowNodeStatus.Completed;
            result.Outputs["results"] = childResults.ToList();

            return result;
        }

        private Dictionary<string, object> ResolveInputs(Dictionary<string, object> inputs)
        {
            var resolved = new Dictionary<string, object>();
            foreach (var input in inputs)
            {
                resolved[input.Key] = ResolveValue(input.Value);
            }
            return resolved;
        }

        private object? ResolveValue(object value)
        {
            if (value is string str && str.StartsWith("${") && str.EndsWith("}"))
            {
                var path = str[2..^1];
                return GetValueFromPath(path);
            }
            return value;
        }

        private object? GetValueFromPath(string path)
        {
            var parts = path.Split('.');
            if (parts.Length < 2)
            {
                return _context.GetValue<object>(path);
            }

            var nodeId = parts[0];
            var property = string.Join(".", parts[1..]);

            if (_context.Results.TryGetValue(nodeId, out var result))
            {
                if (result.Outputs.TryGetValue(property, out var output))
                {
                    return output;
                }
            }

            return _context.GetValue<object>(path);
        }

        private bool EvaluateCondition(string? condition, Dictionary<string, object> inputs)
        {
            if (string.IsNullOrEmpty(condition)) return true;

            // Simple condition evaluation - can be extended with expression parser
            if (condition.Contains("=="))
            {
                var parts = condition.Split("==");
                if (parts.Length == 2)
                {
                    var left = ResolveValue(parts[0].Trim())?.ToString();
                    var right = ResolveValue(parts[1].Trim())?.ToString();
                    return left == right;
                }
            }

            if (condition.Contains("!="))
            {
                var parts = condition.Split("!=");
                if (parts.Length == 2)
                {
                    var left = ResolveValue(parts[0].Trim())?.ToString();
                    var right = ResolveValue(parts[1].Trim())?.ToString();
                    return left != right;
                }
            }

            // Default: try to parse as boolean
            if (bool.TryParse(condition, out var boolValue))
            {
                return boolValue;
            }

            return true;
        }

        public async Task CancelAsync()
        {
            _cts.Cancel();
            Status = WorkflowExecutionStatus.Cancelled;
            await Task.CompletedTask;
        }
    }
}

/// <summary>
/// Workflow builder for fluent workflow construction.
/// 工作流构建器
/// </summary>
public class WorkflowBuilder
{
    private readonly WorkflowDefinition _workflow = new();
    private WorkflowNode? _currentNode;

    public static WorkflowBuilder Create(string name)
    {
        return new WorkflowBuilder { _workflow = { Name = name } };
    }

    public WorkflowBuilder WithDescription(string description)
    {
        _workflow.Description = description;
        return this;
    }

    public WorkflowBuilder WithVersion(string version)
    {
        _workflow.Version = version;
        return this;
    }

    public WorkflowBuilder AddStart(string id = "start")
    {
        var node = new WorkflowNode { Id = id, Name = "Start", Type = WorkflowNodeType.Start };
        _workflow.Nodes.Add(node);
        _workflow.StartNodeId = id;
        _currentNode = node;
        return this;
    }

    public WorkflowBuilder AddEnd(string id = "end")
    {
        var node = new WorkflowNode { Id = id, Name = "End", Type = WorkflowNodeType.End };
        _workflow.Nodes.Add(node);
        return this;
    }

    public WorkflowBuilder AddTask(string id, string name, string? agentName = null, string? toolName = null)
    {
        var node = new WorkflowNode
        {
            Id = id,
            Name = name,
            Type = WorkflowNodeType.Task,
            AgentName = agentName,
            ToolName = toolName
        };
        _workflow.Nodes.Add(node);

        // Connect from current node
        if (_currentNode != null && _currentNode.Id != id)
        {
            node.Dependencies.Add(_currentNode.Id);
        }

        _currentNode = node;
        return this;
    }

    public WorkflowBuilder AddDecision(string id, string name, string condition, string trueBranch, string falseBranch)
    {
        var node = new WorkflowNode
        {
            Id = id,
            Name = name,
            Type = WorkflowNodeType.Decision,
            Condition = condition,
            TrueBranch = trueBranch,
            FalseBranch = falseBranch
        };
        _workflow.Nodes.Add(node);

        if (_currentNode != null)
        {
            node.Dependencies.Add(_currentNode.Id);
        }

        _currentNode = node;
        return this;
    }

    public WorkflowBuilder AddParallel(string id, string name, params WorkflowNode[] children)
    {
        var node = new WorkflowNode
        {
            Id = id,
            Name = name,
            Type = WorkflowNodeType.Parallel,
            Children = children.ToList()
        };
        _workflow.Nodes.Add(node);

        if (_currentNode != null)
        {
            node.Dependencies.Add(_currentNode.Id);
        }

        _currentNode = node;
        return this;
    }

    public WorkflowBuilder WithInput(string name, string type = "string", bool required = true, object? defaultValue = null)
    {
        _workflow.Inputs.Add(new WorkflowInput
        {
            Name = name,
            Type = type,
            Required = required,
            DefaultValue = defaultValue
        });
        return this;
    }

    public WorkflowBuilder WithOutput(string name, string source)
    {
        _workflow.Outputs.Add(new WorkflowOutput
        {
            Name = name,
            Source = source
        });
        return this;
    }

    public WorkflowBuilder Connect(string fromId, string toId)
    {
        var toNode = _workflow.GetNode(toId);
        if (toNode != null && !toNode.Dependencies.Contains(fromId))
        {
            toNode.Dependencies.Add(fromId);
        }
        return this;
    }

    public WorkflowDefinition Build()
    {
        _workflow.BuildConnections();
        return _workflow;
    }
}
