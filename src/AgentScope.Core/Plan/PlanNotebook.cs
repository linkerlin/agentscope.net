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
/// Event arguments for plan execution status change events.
/// 计划执行状态变更事件的事件参数。
/// Corresponds to Java: io.agentscope.core.plan.PlanExecutionEventArgs
/// 对应 Java: io.agentscope.core.plan.PlanExecutionEventArgs
/// </summary>
public class PlanExecutionEventArgs : EventArgs
{
    /// <summary>
    /// The ID of the plan being executed.
    /// 正在执行的计划 ID。
    /// </summary>
    public string PlanId { get; set; } = "";

    /// <summary>
    /// The ID of the node whose status changed.
    /// 状态发生变更的节点 ID。
    /// </summary>
    public string NodeId { get; set; } = "";

    /// <summary>
    /// The name of the node whose status changed.
    /// 状态发生变更的节点名称。
    /// </summary>
    public string NodeName { get; set; } = "";

    /// <summary>
    /// The previous status before the change.
    /// 变更前的状态。
    /// </summary>
    public PlanStatus OldStatus { get; set; }

    /// <summary>
    /// The new status after the change.
    /// 变更后的新状态。
    /// </summary>
    public PlanStatus NewStatus { get; set; }

    /// <summary>
    /// Optional message providing additional context about the status change.
    /// 可选消息，提供关于状态变更的额外上下文。
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Timestamp when the status change occurred.
    /// 状态变更发生的时间戳。
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Delegate for handling plan execution events.
/// 处理计划执行事件的委托。
/// </summary>
/// <param name="sender">The source of the event. 事件源。</param>
/// <param name="e">Event arguments containing status change details.
/// 包含状态变更详情的事件参数。</param>
public delegate void PlanExecutionEventHandler(object sender, PlanExecutionEventArgs e);

/// <summary>
/// Interface for plan node executors.
/// 计划节点执行器接口。
/// Corresponds to Java: io.agentscope.core.plan.IPlanExecutor
/// 对应 Java: io.agentscope.core.plan.IPlanExecutor
/// </summary>
public interface IPlanExecutor
{
    /// <summary>
    /// Executes a single plan node asynchronously.
    /// 异步执行单个计划节点。
    /// </summary>
    /// <param name="node">The plan node to execute. 要执行的计划节点。</param>
    /// <param name="context">The execution context containing agents, tools, and state.
    /// 包含 Agent、工具和状态的执行上下文。</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.
    /// 用于取消操作的取消令牌。</param>
    /// <returns>A PlanExecutionResult with success/failure information.
    /// 包含成功/失败信息的 PlanExecutionResult。</returns>
    Task<PlanExecutionResult> ExecuteNodeAsync(PlanNode node, PlanContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents the result of executing a plan node.
/// 表示计划节点执行的结果。
/// Corresponds to Java: io.agentscope.core.plan.PlanExecutionResult
/// 对应 Java: io.agentscope.core.plan.PlanExecutionResult
/// </summary>
public class PlanExecutionResult
{
    /// <summary>
    /// Whether the execution was successful.
    /// 执行是否成功。
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Text output from the execution.
    /// 执行的文本输出。
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// Error message if execution failed.
    /// 执行失败时的错误消息。
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// Structured output data from the execution.
    /// 执行产生的结构化输出数据。
    /// </summary>
    public Dictionary<string, object> Outputs { get; set; } = new();

    /// <summary>
    /// Duration of the execution.
    /// 执行持续时间。
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// Context object for plan execution, providing access to agents, tools, and shared state.
/// 计划执行上下文对象，提供对 Agent、工具和共享状态的访问。
/// Corresponds to Java: io.agentscope.core.plan.PlanContext
/// 对应 Java: io.agentscope.core.plan.PlanContext
/// </summary>
public class PlanContext
{
    /// <summary>
    /// The plan being executed.
    /// 正在执行的计划。
    /// </summary>
    public Plan Plan { get; set; } = new();

    /// <summary>
    /// Shared state dictionary accessible across all nodes during execution.
    /// 执行期间所有节点可访问的共享状态字典。
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// Dictionary of available agents for task execution, keyed by agent name.
    /// 可用于任务执行的 Agent 字典，以 Agent 名称为键。
    /// </summary>
    public Dictionary<string, IAgent> Agents { get; set; } = new();

    /// <summary>
    /// Dictionary of available tools, keyed by tool name.
    /// 可用工具字典，以工具名称为键。
    /// </summary>
    public Dictionary<string, ITool> Tools { get; set; } = new();

    /// <summary>
    /// Default agent used for tasks without a specific agent assignment.
    /// 用于未指定 Agent 的任务的默认 Agent。
    /// </summary>
    public IAgent? DefaultAgent { get; set; }

    /// <summary>
    /// Execution options controlling parallelism, retry, timeout, etc.
    /// 控制并行度、重试、超时等的执行选项。
    /// </summary>
    public PlanExecutionOptions Options { get; set; } = new();

    /// <summary>
    /// Gets an agent by name, falling back to the default agent if not found.
    /// 按名称获取 Agent，如果未找到则回退到默认 Agent。
    /// </summary>
    /// <param name="name">The agent name to look up. 要查找的 Agent 名称。</param>
    /// <returns>The matching agent or the default agent. 匹配的 Agent 或默认 Agent。</returns>
    public IAgent? GetAgent(string? name)
    {
        if (string.IsNullOrEmpty(name)) return DefaultAgent;
        return Agents.TryGetValue(name, out var agent) ? agent : DefaultAgent;
    }

    /// <summary>
    /// Gets a tool by name.
    /// 按名称获取工具。
    /// </summary>
    /// <param name="name">The tool name to look up. 要查找的工具名称。</param>
    /// <returns>The matching tool, or null if not found. 匹配的工具，如果未找到则返回 null。</returns>
    public ITool? GetTool(string? name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return Tools.TryGetValue(name, out var tool) ? tool : null;
    }
}

/// <summary>
/// Configuration options for plan execution behavior.
/// 计划执行行为的配置选项。
/// Corresponds to Java: io.agentscope.core.plan.PlanExecutionOptions
/// 对应 Java: io.agentscope.core.plan.PlanExecutionOptions
/// </summary>
public class PlanExecutionOptions
{
    /// <summary>
    /// Maximum number of nodes to execute in parallel. Default is 5.
    /// 最大并行执行节点数。默认为 5。
    /// </summary>
    public int MaxParallelism { get; set; } = 5;

    /// <summary>
    /// Whether to continue execution when a node fails. Default is false.
    /// 节点失败时是否继续执行。默认为 false。
    /// </summary>
    public bool ContinueOnError { get; set; } = false;

    /// <summary>
    /// Whether to automatically retry failed nodes. Default is true.
    /// 是否自动重试失败的节点。默认为 true。
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Optional global timeout for the entire plan execution.
    /// 整个计划执行的可选全局超时时间。
    /// </summary>
    public TimeSpan? GlobalTimeout { get; set; }

    /// <summary>
    /// Whether to propagate node outputs to the parent state. Default is true.
    /// 是否将节点输出传播到父状态。默认为 true。
    /// </summary>
    public bool PropagateOutputs { get; set; } = true;
}

/// <summary>
/// PlanNotebook - Core plan management and execution engine.
/// PlanNotebook - 计划管理与执行引擎核心。
/// Manages the lifecycle of plans, including creation, modification, and execution.
/// 管理计划的完整生命周期，包括创建、修改和执行。
/// Corresponds to Java: io.agentscope.core.plan.PlanNotebook
/// 对应 Java: io.agentscope.core.plan.PlanNotebook
/// </summary>
public class PlanNotebook : IPlanExecutor
{
    /// <summary>
    /// Internal storage for all managed plans, keyed by plan ID.
    /// 所有管理的计划的内部存储，以计划 ID 为键。
    /// </summary>
    private readonly Dictionary<string, Plan> _plans = new();

    /// <summary>
    /// Semaphore for serializing plan execution to prevent concurrent execution conflicts.
    /// 用于序列化计划执行以防止并发执行冲突的信号量。
    /// </summary>
    private readonly SemaphoreSlim _executionLock = new(1, 1);

    /// <summary>
    /// Event raised when a node's execution status changes.
    /// 当节点执行状态变更时触发的事件。
    /// </summary>
    public event PlanExecutionEventHandler? NodeStatusChanged;

    /// <summary>
    /// Event raised when a plan execution completes (success, failure, or cancellation).
    /// 当计划执行完成（成功、失败或取消）时触发的事件。
    /// </summary>
    public event EventHandler<string>? PlanCompleted;

    /// <summary>
    /// Creates a new plan with the specified name and optional description.
    /// 使用指定的名称和可选描述创建一个新计划。
    /// </summary>
    /// <param name="name">The name of the plan. 计划名称。</param>
    /// <param name="description">Optional description of the plan. 计划的可选描述。</param>
    /// <returns>The newly created Plan instance. 新创建的 Plan 实例。</returns>
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
    /// Retrieves a plan by its unique identifier.
    /// 通过唯一标识符检索计划。
    /// </summary>
    /// <param name="id">The plan ID to look up. 要查找的计划 ID。</param>
    /// <returns>The matching Plan, or null if not found. 匹配的计划，如果未找到则返回 null。</returns>
    public Plan? GetPlan(string id)
    {
        return _plans.TryGetValue(id, out var plan) ? plan : null;
    }

    /// <summary>
    /// Gets a read-only collection of all managed plans.
    /// 获取所有管理的计划的只读集合。
    /// </summary>
    /// <returns>Read-only collection of all plans. 所有计划的只读集合。</returns>
    public IReadOnlyCollection<Plan> GetAllPlans()
    {
        return _plans.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Deletes a plan by its ID.
    /// 通过 ID 删除计划。
    /// </summary>
    /// <param name="id">The ID of the plan to delete. 要删除的计划 ID。</param>
    /// <returns>True if the plan was found and removed; otherwise false.
    /// 如果找到并移除了计划则返回 true；否则返回 false。</returns>
    public bool DeletePlan(string id)
    {
        return _plans.Remove(id);
    }

    /// <summary>
    /// Adds a task node as a child of the specified parent node.
    /// 添加一个任务节点作为指定父节点的子节点。
    /// </summary>
    /// <param name="plan">The plan to modify. 要修改的计划。</param>
    /// <param name="parentId">The ID of the parent node. 父节点 ID。</param>
    /// <param name="name">The name of the new task node. 新任务节点的名称。</param>
    /// <param name="description">Optional description of the task. 任务的可选描述。</param>
    /// <param name="assignedAgent">Optional agent name to assign to this task.
    /// 分配给此任务的可选 Agent 名称。</param>
    /// <param name="toolName">Optional tool name to use for this task.
    /// 此任务使用的可选工具名称。</param>
    /// <returns>The newly created PlanNode. 新创建的 PlanNode。</returns>
    /// <exception cref="ArgumentException">Thrown if the parent node is not found.
    /// 如果未找到父节点则抛出。</exception>
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
    /// Adds a sub-plan node as a child of the specified parent node.
    /// 添加一个子计划节点作为指定父节点的子节点。
    /// </summary>
    /// <param name="plan">The plan to modify. 要修改的计划。</param>
    /// <param name="parentId">The ID of the parent node. 父节点 ID。</param>
    /// <param name="name">The name of the new sub-plan node. 新子计划节点的名称。</param>
    /// <param name="description">Optional description of the sub-plan. 子计划的可选描述。</param>
    /// <returns>The newly created PlanNode. 新创建的 PlanNode。</returns>
    /// <exception cref="ArgumentException">Thrown if the parent node is not found.
    /// 如果未找到父节点则抛出。</exception>
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
    /// Adds a dependency relationship between two nodes.
    /// 在两个节点之间添加依赖关系。
    /// </summary>
    /// <param name="plan">The plan containing the nodes. 包含节点的计划。</param>
    /// <param name="nodeId">The ID of the node that depends on another.
    /// 依赖其他节点的节点 ID。</param>
    /// <param name="dependsOnId">The ID of the node that must complete first.
    /// 必须先完成的节点 ID。</param>
    /// <exception cref="ArgumentException">Thrown if either node is not found.
    /// 如果任一节点未找到则抛出。</exception>
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
    /// Executes a plan asynchronously, managing the full execution lifecycle.
    /// 异步执行计划，管理完整的执行生命周期。
    /// </summary>
    /// <param name="plan">The plan to execute. 要执行的计划。</param>
    /// <param name="context">The execution context with agents, tools, and options.
    /// 包含 Agent、工具和选项的执行上下文。</param>
    /// <param name="cancellationToken">Cancellation token to cancel execution.
    /// 用于取消执行的取消令牌。</param>
    /// <returns>A PlanExecutionSummary with execution statistics.
    /// 包含执行统计信息的 PlanExecutionSummary。</returns>
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
            // Handle cancellation gracefully - nodes already marked as cancelled
            // 优雅处理取消 - 节点已标记为已取消
        }

        plan.CompletedAt = DateTime.UtcNow;
        plan.Status = plan.IsSuccessful() ? PlanStatus.Completed : 
                      plan.GetAllNodes().Values.Any(n => n.Status == PlanStatus.Failed) ? PlanStatus.Failed :
                      PlanStatus.Cancelled;

        PlanCompleted?.Invoke(this, plan.Id);
        
        return plan.GetExecutionSummary();
    }

    /// <summary>
    /// Core execution loop that manages node scheduling, parallelism, and completion tracking.
    /// 核心执行循环，管理节点调度、并行度和完成跟踪。
    /// </summary>
    /// <param name="plan">The plan being executed. 正在执行的计划。</param>
    /// <param name="context">The execution context. 执行上下文。</param>
    /// <param name="allNodes">Flat dictionary of all nodes in the plan.
    /// 计划中所有节点的扁平字典。</param>
    /// <param name="cancellationToken">Cancellation token. 取消令牌。</param>
    private async Task ExecuteNodesAsync(Plan plan, PlanContext context, 
        Dictionary<string, PlanNode> allNodes, CancellationToken cancellationToken)
    {
        var executingNodes = new HashSet<string>();
        var completedNodes = new HashSet<string>();

        while (completedNodes.Count < allNodes.Count)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                // Cancel all remaining pending nodes
                // 取消所有剩余的待处理节点
                foreach (var node in allNodes.Values.Where(n => n.Status == PlanStatus.Pending))
                {
                    node.MarkCancelled("Plan execution was cancelled");
                    OnNodeStatusChanged(plan.Id, node, PlanStatus.Pending, PlanStatus.Cancelled);
                }
                throw new OperationCanceledException();
            }

            // Find nodes that are ready to execute (pending + dependencies satisfied)
            // 查找已准备好执行的节点（待处理 + 依赖已满足）
            var readyNodes = allNodes.Values
                .Where(n => n.Status == PlanStatus.Pending && 
                           n.CanExecute(allNodes) && 
                           !executingNodes.Contains(n.Id))
                .ToList();

            if (readyNodes.Count == 0)
            {
                // Check for deadlock: nodes stuck waiting for unsatisfied dependencies
                // 检查死锁：节点因依赖未满足而卡住
                var stuckNodes = allNodes.Values
                    .Where(n => n.Status == PlanStatus.Pending && 
                               !n.AreDependenciesSatisfied(allNodes))
                    .ToList();

                if (stuckNodes.Any())
                {
                    throw new PlanExecutionException($"Dependencies cannot be satisfied for nodes: {string.Join(", ", stuckNodes.Select(n => n.Name))}");
                }

                // Check if all nodes are in terminal states (no pending nodes left)
                // 检查所有节点是否都处于终止状态（没有剩余的待处理节点）
                if (allNodes.Values.All(n => n.Status != PlanStatus.Pending))
                {
                    break;
                }

                // Brief delay to allow executing nodes to complete
                // 短暂延迟以允许执行中的节点完成
                await Task.Delay(100, cancellationToken);
                continue;
            }

            // Limit the number of concurrent executions based on MaxParallelism
            // 根据 MaxParallelism 限制并发执行数
            var availableSlots = context.Options.MaxParallelism - executingNodes.Count;
            var nodesToStart = readyNodes.Take(availableSlots).ToList();

            // Start execution tasks for ready nodes
            // 为准备好的节点启动执行任务
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
                        // Retry the node if retry is enabled and max retries not reached
                        // 如果启用了重试且未达到最大重试次数，则重试节点
                        node.RetryCount++;
                        node.Status = PlanStatus.Pending; // Reset to pending for retry
                        executingNodes.Remove(node.Id);
                    }
                    else
                    {
                        node.MarkFailed(result.Error ?? "Execution failed");
                        completedNodes.Add(node.Id);
                        OnNodeStatusChanged(plan.Id, node, PlanStatus.InProgress, PlanStatus.Failed);

                        if (!context.Options.ContinueOnError)
                        {
                            // Cancel all remaining pending nodes on failure
                            // 失败时取消所有剩余的待处理节点
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
                // Wait for at least one task to complete before re-evaluating
                // 等待至少一个任务完成后再重新评估
                await Task.WhenAny(executionTasks);
            }
        }
    }

    /// <summary>
    /// Executes a single plan node, dispatching to the appropriate handler based on node type.
    /// 执行单个计划节点，根据节点类型分派到相应的处理程序。
    /// </summary>
    /// <param name="node">The node to execute. 要执行的节点。</param>
    /// <param name="context">The execution context. 执行上下文。</param>
    /// <param name="cancellationToken">Cancellation token. 取消令牌。</param>
    /// <returns>Execution result with success/failure information.
    /// 包含成功/失败信息的执行结果。</returns>
    public virtual async Task<PlanExecutionResult> ExecuteNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        node.MarkInProgress();
        OnNodeStatusChanged(context.Plan.Id, node, PlanStatus.Pending, PlanStatus.InProgress);

        try
        {
            // Dispatch to the appropriate handler based on node type
            // 根据节点类型分派到相应的处理程序
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

    /// <summary>
    /// Executes a task node using either a specified tool or an assigned agent.
    /// 使用指定的工具或分配的 Agent 执行任务节点。
    /// </summary>
    /// <param name="node">The task node to execute. 要执行的任务节点。</param>
    /// <param name="context">The execution context. 执行上下文。</param>
    /// <param name="cancellationToken">Cancellation token. 取消令牌。</param>
    /// <returns>Execution result with the task output.
    /// 包含任务输出的执行结果。</returns>
    private async Task<PlanExecutionResult> ExecuteTaskNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;

        // If a tool is specified, use it to execute the task
        // 如果指定了工具，则使用工具执行任务
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

        // Otherwise use an agent to execute the task
        // 否则使用 Agent 执行任务
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

        // Build a message from the node description and send it to the agent
        // 从节点描述构建消息并发送给 Agent
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

    /// <summary>
    /// Executes a sub-plan node by running all its child nodes.
    /// 通过运行所有子节点来执行子计划节点。
    /// </summary>
    /// <param name="node">The sub-plan node to execute. 要执行的子计划节点。</param>
    /// <param name="context">The execution context. 执行上下文。</param>
    /// <param name="cancellationToken">Cancellation token. 取消令牌。</param>
    /// <returns>Execution result indicating whether all children completed successfully.
    /// 指示所有子节点是否成功完成的执行结果。</returns>
    private async Task<PlanExecutionResult> ExecuteSubPlanNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken)
    {
        // Collect all child nodes into a flat dictionary for execution
        // 将所有子节点收集到扁平字典中以供执行
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

    /// <summary>
    /// Executes child nodes sequentially in order, stopping on first failure if ContinueOnError is false.
    /// 按顺序依次执行子节点，如果 ContinueOnError 为 false 则在第一个失败时停止。
    /// </summary>
    /// <param name="node">The sequential node to execute. 要执行的顺序节点。</param>
    /// <param name="context">The execution context. 执行上下文。</param>
    /// <param name="cancellationToken">Cancellation token. 取消令牌。</param>
    /// <returns>Execution result indicating success or failure.
    /// 指示成功或失败的执行结果。</returns>
    private async Task<PlanExecutionResult> ExecuteSequentialNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken)
    {
        // Execute children one by one in order
        // 按顺序逐个执行子节点
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

    /// <summary>
    /// Executes all child nodes in parallel.
    /// 并行执行所有子节点。
    /// </summary>
    /// <param name="node">The parallel node to execute. 要执行的并行节点。</param>
    /// <param name="context">The execution context. 执行上下文。</param>
    /// <param name="cancellationToken">Cancellation token. 取消令牌。</param>
    /// <returns>Execution result indicating whether all children succeeded.
    /// 指示所有子节点是否成功的执行结果。</returns>
    private async Task<PlanExecutionResult> ExecuteParallelNodeAsync(PlanNode node, PlanContext context, 
        CancellationToken cancellationToken)
    {
        // Execute all children concurrently
        // 并发执行所有子节点
        var tasks = node.Children.Select(child => ExecuteNodeAsync(child, context, cancellationToken));
        var results = await Task.WhenAll(tasks);

        var success = results.All(r => r.Success);
        return new PlanExecutionResult { Success = success };
    }

    /// <summary>
    /// Recursively collects all nodes from the given node into a flat dictionary.
    /// 递归收集从给定节点开始的所有节点到扁平字典中。
    /// </summary>
    /// <param name="node">Current node to collect. 当前要收集的节点。</param>
    /// <param name="nodes">Dictionary to populate with node ID to node mappings.
    /// 要填充的节点 ID 到节点映射的字典。</param>
    private void CollectNodes(PlanNode node, Dictionary<string, PlanNode> nodes)
    {
        nodes[node.Id] = node;
        foreach (var child in node.Children)
        {
            CollectNodes(child, nodes);
        }
    }

    /// <summary>
    /// Raises the NodeStatusChanged event with the provided status change details.
    /// 使用提供的状态变更详情触发 NodeStatusChanged 事件。
    /// </summary>
    /// <param name="planId">The ID of the plan. 计划 ID。</param>
    /// <param name="node">The node whose status changed. 状态发生变更的节点。</param>
    /// <param name="oldStatus">The previous status. 变更前的状态。</param>
    /// <param name="newStatus">The new status. 变更后的新状态。</param>
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
/// Exception thrown when plan execution encounters an error.
/// 计划执行遇到错误时抛出的异常。
/// Corresponds to Java: io.agentscope.core.plan.PlanExecutionException
/// 对应 Java: io.agentscope.core.plan.PlanExecutionException
/// </summary>
public class PlanExecutionException : System.Exception
{
    /// <summary>
    /// Initializes a new instance of PlanExecutionException with a message.
    /// 使用消息初始化 PlanExecutionException 的新实例。
    /// </summary>
    /// <param name="message">The error message. 错误消息。</param>
    public PlanExecutionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of PlanExecutionException with a message and inner exception.
    /// 使用消息和内部异常初始化 PlanExecutionException 的新实例。
    /// </summary>
    /// <param name="message">The error message. 错误消息。</param>
    /// <param name="innerException">The inner exception that caused this error.
    /// 导致此错误的内部异常。</param>
    public PlanExecutionException(string message, System.Exception innerException) : base(message, innerException) { }
}
