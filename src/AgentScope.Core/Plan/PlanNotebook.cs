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
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;
using AgentScope.Core.Tool;

namespace AgentScope.Core.Plan;

/// <summary>
/// Plan execution event args.
/// </summary>
public class PlanExecutionEventArgs : EventArgs
{
    public string PlanId { get; set; } = "";
    public string NodeId { get; set; } = "";
    public string NodeName { get; set; } = "";
    public PlanStatus OldStatus { get; set; }
    public PlanStatus NewStatus { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Delegate for plan execution events.
/// </summary>
public delegate void PlanExecutionEventHandler(object sender, PlanExecutionEventArgs e);

/// <summary>
/// Plan executor interface.
/// </summary>
public interface IPlanExecutor
{
    /// <summary>
    /// Executes a plan node.
    /// </summary>
    Task<PlanExecutionResult> ExecuteNodeAsync(PlanNode node, PlanContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Plan execution result.
/// </summary>
public class PlanExecutionResult
{
    public bool Success { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public Dictionary<string, object> Outputs { get; set; } = new();
    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// Context for plan execution.
/// </summary>
public class PlanContext
{
    /// <summary>
    /// The plan being executed.
    /// </summary>
    public Plan Plan { get; set; } = new();

    /// <summary>
    /// Shared state across all nodes.
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// Available agents for task execution.
    /// </summary>
    public Dictionary<string, IAgent> Agents { get; set; } = new();

    /// <summary>
    /// Available tools.
    /// </summary>
    public Dictionary<string, ITool> Tools { get; set; } = new();

    /// <summary>
    /// Default agent for tasks without specific assignment.
    /// </summary>
    public IAgent? DefaultAgent { get; set; }

    /// <summary>
    /// Execution options.
    /// </summary>
    public PlanExecutionOptions Options { get; set; } = new();

    /// <summary>
    /// Gets an agent by name or returns default.
    /// </summary>
    public IAgent? GetAgent(string? name)
    {
        if (string.IsNullOrEmpty(name)) return DefaultAgent;
        return Agents.TryGetValue(name, out var agent) ? agent : DefaultAgent;
    }

    /// <summary>
    /// Gets a tool by name.
    /// </summary>
    public ITool? GetTool(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return Tools.TryGetValue(name, out var tool) ? tool : null;
    }
}

/// <summary>
/// Plan execution options.
/// </summary>
public class PlanExecutionOptions
{
    /// <summary>
    /// Maximum parallel execution count.
    /// </summary>
    public int MaxParallelism { get; set; } = 5;

    /// <summary>
    /// Whether to continue on node failure.
    /// </summary>
    public bool ContinueOnError { get; set; } = false;

    /// <summary>
    /// Whether to enable automatic retry.
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Global timeout for the entire plan.
    /// </summary>
    public TimeSpan? GlobalTimeout { get; set; }

    /// <summary>
    /// Whether to propagate outputs to parent state.
    /// </summary>
    public bool PropagateOutputs { get; set; } = true;
}

/// <summary>
/// PlanNotebook - Core plan management and execution engine.
/// Plan 管理与执行引擎核心
/// 
/// Java参考: io.agentscope.core.plan.PlanNotebook
/// </summary>
public class PlanNotebook : IPlanExecutor
{
    private readonly Dictionary<string, Plan> _plans = new();
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    /// <summary>
    /// Event raised when a node status changes.
    /// </summary>
    public event PlanExecutionEventHandler? NodeStatusChanged;

    /// <summary>
    /// Event raised when plan execution completes.
    /// </summary>
    public event EventHandler<string>? PlanCompleted;

    /// <summary>
    /// Creates a new plan.
    /// </summary>
    public Plan CreatePlan(string name, string? description = null)
    {
        var plan = new Plan
        {
            Name = name,
            Description = description,
            RootNode = new PlanNode
            {
                Name = name,
                Type = PlanNodeType.Sequential,
                Description = description
            }
        };

        _plans[plan.Id] = plan;
        return plan;
    }

    /// <summary>
    /// Gets a plan by ID.
    /// </summary>
    public Plan? GetPlan(string id)
    {
        return _plans.TryGetValue(id, out var plan) ? plan : null;
    }

    /// <summary>
    /// Gets all plans.
    /// </summary>
    public IReadOnlyCollection<Plan> GetAllPlans()
    {
        return _plans.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Deletes a plan.
    /// </summary>
    public bool DeletePlan(string id)
    {
        return _plans.Remove(id);
    }

    /// <summary>
    /// Adds a task node to a parent node.
    /// </summary>
    public PlanNode AddTask(Plan plan, string parentId, string name, string? description = null, 
                           string? assignedAgent = null, string? toolName = null)
    {
        var parent = plan.FindNode(parentId);
        if (parent == null)
        {
            throw new ArgumentException($"Parent node {parentId} not found", nameof(parentId));
        }

        var node = new PlanNode
        {
            Name = name,
            Description = description,
            Type = PlanNodeType.Task,
            ParentId = parentId,
            AssignedAgent = assignedAgent,
            ToolName = toolName
        };

        parent.Children.Add(node);
        plan.UpdatedAt = DateTime.UtcNow;
        
        return node;
    }

    /// <summary>
    /// Adds a sub-plan node.
    /// </summary>
    public PlanNode AddSubPlan(Plan plan, string parentId, string name, string? description = null)
    {
        var parent = plan.FindNode(parentId);
        if (parent == null)
        {
            throw new ArgumentException($"Parent node {parentId} not found", nameof(parentId));
        }

        var node = new PlanNode
        {
            Name = name,
            Description = description,
            Type = PlanNodeType.SubPlan,
            ParentId = parentId
        };

        parent.Children.Add(node);
        plan.UpdatedAt = DateTime.UtcNow;
        
        return node;
    }

    /// <summary>
    /// Adds a dependency between nodes.
    /// </summary>
    public void AddDependency(Plan plan, string nodeId, string dependsOnId)
    {
        var node = plan.FindNode(nodeId);
        if (node == null)
        {
            throw new ArgumentException($"Node {nodeId} not found", nameof(nodeId));
        }

        if (plan.FindNode(dependsOnId) == null)
        {
            throw new ArgumentException($"Dependency node {dependsOnId} not found", nameof(dependsOnId));
        }

        if (!node.Dependencies.Contains(dependsOnId))
        {
            node.Dependencies.Add(dependsOnId);
        }

        plan.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Executes a plan.
    /// </summary>
    public async Task<PlanExecutionSummary> ExecutePlanAsync(Plan plan, PlanContext context, 
        CancellationToken cancellationToken = default)
    {
        plan.Status = PlanStatus.InProgress;
        var allNodes = plan.GetAllNodes();

        try
        {
            if (context.Options.GlobalTimeout.HasValue)
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(context.Options.GlobalTimeout.Value);
                await ExecuteNodesAsync(plan, context, allNodes, cts.Token);
            }
            else
            {
                await ExecuteNodesAsync(plan, context, allNodes, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Handle cancellation
        }

        plan.CompletedAt = DateTime.UtcNow;
        plan.Status = plan.IsSuccessful() ? PlanStatus.Completed : 
                      plan.GetAllNodes().Values.Any(n => n.Status == PlanStatus.Failed) ? PlanStatus.Failed :
                      PlanStatus.Cancelled;

        PlanCompleted?.Invoke(this, plan.Id);
        
        return plan.GetExecutionSummary();
    }

    private async Task ExecuteNodesAsync(Plan plan, PlanContext context, 
        Dictionary<string, PlanNode> allNodes, CancellationToken cancellationToken)
    {
        var executingNodes = new HashSet<string>();
        var completedNodes = new HashSet<string>();

        while (completedNodes.Count < allNodes.Count)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Cancel remaining nodes
                foreach (var node in allNodes.Values.Where(n => n.Status == PlanStatus.Pending))
                {
                    node.MarkCancelled("Plan execution was cancelled");
                    OnNodeStatusChanged(plan.Id, node, PlanStatus.Pending, PlanStatus.Cancelled);
                }
                throw new OperationCanceledException();
            }

            // Find ready nodes
            var readyNodes = allNodes.Values
                .Where(n => n.Status == PlanStatus.Pending && 
                           n.CanExecute(allNodes) && 
                           !executingNodes.Contains(n.Id))
                .ToList();

            if (readyNodes.Count == 0)
            {
                // Check if we're stuck
                var stuckNodes = allNodes.Values
                    .Where(n => n.Status == PlanStatus.Pending && 
                               !n.AreDependenciesSatisfied(allNodes))
                    .ToList();

                if (stuckNodes.Any())
                {
                    throw new PlanExecutionException($"Dependencies cannot be satisfied for nodes: {string.Join(", ", stuckNodes.Select(n => n.Name))}");
                }

                // Check if all nodes are done or executing
                if (allNodes.Values.All(n => n.Status != PlanStatus.Pending))
                {
                    break;
                }

                // Wait a bit for executing nodes to complete
                await Task.Delay(100, cancellationToken);
                continue;
            }

            // Limit parallelism
            var availableSlots = context.Options.MaxParallelism - executingNodes.Count;
            var nodesToStart = readyNodes.Take(availableSlots).ToList();

            // Start execution of ready nodes
            var executionTasks = nodesToStart.Select(async node =>
            {
                executingNodes.Add(node.Id);
                
                try
                {
                    var result = await ExecuteNodeAsync(node, context, cancellationToken);
                    
                    if (result.Success)
                    {
                        node.MarkCompleted(result.Output);
                        completedNodes.Add(node.Id);
                        OnNodeStatusChanged(plan.Id, node, PlanStatus.InProgress, PlanStatus.Completed);
                    }
                    else if (context.Options.EnableRetry && node.RetryCount < node.MaxRetries)
                    {
                        node.RetryCount++;
                        node.Status = PlanStatus.Pending; // Retry
                        executingNodes.Remove(node.Id);
                    }
                    else
                    {
                        node.MarkFailed(result.Error ?? "Execution failed");
                        completedNodes.Add(node.Id);
                        OnNodeStatusChanged(plan.Id, node, PlanStatus.InProgress, PlanStatus.Failed);

                        if (!context.Options.ContinueOnError)
                        {
                            // Cancel remaining nodes
                            foreach (var remaining in allNodes.Values.Where(n => n.Status == PlanStatus.Pending))
                            {
                                remaining.MarkCancelled("Previous node failed");
                                OnNodeStatusChanged(plan.Id, remaining, PlanStatus.Pending, PlanStatus.Cancelled);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    node.MarkCancelled("Execution was cancelled");
                    OnNodeStatusChanged(plan.Id, node, PlanStatus.InProgress, PlanStatus.Cancelled);
                    throw;
                }
                finally
                {
                    executingNodes.Remove(node.Id);
                }
            }).ToList();

            if (executionTasks.Any())
            {
                await Task.WhenAny(executionTasks);
            }
        }
    }

    /// <summary>
    /// Executes a single plan node.
    /// </summary>
    public virtual async Task<PlanExecutionResult> ExecuteNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        node.MarkInProgress();
        OnNodeStatusChanged(context.Plan.Id, node, PlanStatus.Pending, PlanStatus.InProgress);

        try
        {
            // Handle different node types
            return node.Type switch
            {
                PlanNodeType.Task => await ExecuteTaskNodeAsync(node, context, cancellationToken),
                PlanNodeType.SubPlan => await ExecuteSubPlanNodeAsync(node, context, cancellationToken),
                PlanNodeType.Sequential => await ExecuteSequentialNodeAsync(node, context, cancellationToken),
                PlanNodeType.Parallel => await ExecuteParallelNodeAsync(node, context, cancellationToken),
                _ => new PlanExecutionResult 
                { 
                    Success = false, 
                    Error = $"Unknown node type: {node.Type}" 
                }
            };
        }
        catch (System.Exception ex)
        {
            return new PlanExecutionResult
            {
                Success = false,
                Error = ex.Message,
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }
    }

    private async Task<PlanExecutionResult> ExecuteTaskNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        // If a tool is specified, use it
        if (!string.IsNullOrEmpty(node.ToolName))
        {
            var tool = context.GetTool(node.ToolName);
            if (tool != null)
            {
                var toolResult = await tool.ExecuteAsync(node.Inputs);
                
                return new PlanExecutionResult
                {
                    Success = toolResult.Success,
                    Output = toolResult.Result?.ToString(),
                    Error = toolResult.Error,
                    Outputs = toolResult.Result is Dictionary<string, object> dict ? dict : new(),
                    ExecutionTime = DateTime.UtcNow - startTime
                };
            }
        }

        // Otherwise use an agent
        var agent = context.GetAgent(node.AssignedAgent);
        if (agent == null)
        {
            return new PlanExecutionResult
            {
                Success = false,
                Error = "No agent available to execute task",
                ExecutionTime = DateTime.UtcNow - startTime
            };
        }

        var message = Msg.Builder()
            .Role("user")
            .TextContent(node.Description ?? node.Name)
            .Build();

        var response = await agent.CallAsync(message);

        return new PlanExecutionResult
        {
            Success = true,
            Output = response.GetTextContent(),
            ExecutionTime = DateTime.UtcNow - startTime
        };
    }

    private async Task<PlanExecutionResult> ExecuteSubPlanNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken)
    {
        // Execute children of the sub-plan
        var allNodes = new Dictionary<string, PlanNode>();
        foreach (var child in node.Children)
        {
            CollectNodes(child, allNodes);
        }

        await ExecuteNodesAsync(context.Plan, context, allNodes, cancellationToken);

        return new PlanExecutionResult
        {
            Success = node.Children.All(c => c.Status == PlanStatus.Completed)
        };
    }

    private async Task<PlanExecutionResult> ExecuteSequentialNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken)
    {
        // Execute children in order
        foreach (var child in node.Children)
        {
            var result = await ExecuteNodeAsync(child, context, cancellationToken);
            if (!result.Success && !context.Options.ContinueOnError)
            {
                return result;
            }
        }

        return new PlanExecutionResult { Success = true };
    }

    private async Task<PlanExecutionResult> ExecuteParallelNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken)
    {
        // Execute children in parallel
        var tasks = node.Children.Select(child => ExecuteNodeAsync(child, context, cancellationToken));
        var results = await Task.WhenAll(tasks);

        var success = results.All(r => r.Success);
        return new PlanExecutionResult { Success = success };
    }

    private void CollectNodes(PlanNode node, Dictionary<string, PlanNode> nodes)
    {
        nodes[node.Id] = node;
        foreach (var child in node.Children)
        {
            CollectNodes(child, nodes);
        }
    }

    private void OnNodeStatusChanged(string planId, PlanNode node, PlanStatus oldStatus, PlanStatus newStatus)
    {
        NodeStatusChanged?.Invoke(this, new PlanExecutionEventArgs
        {
            PlanId = planId,
            NodeId = node.Id,
            NodeName = node.Name,
            OldStatus = oldStatus,
            NewStatus = newStatus
        });
    }
}

/// <summary>
/// Plan execution exception.
/// </summary>
public class PlanExecutionException : System.Exception
{
    public PlanExecutionException(string message) : base(message) { }
    public PlanExecutionException(string message, System.Exception innerException) : base(message, innerException) { }
}
