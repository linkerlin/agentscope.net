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
using System.Text.Json.Serialization;

namespace AgentScope.Core.Plan;

/// <summary>
/// Plan status enumeration.
/// Plan（计划）状态枚举，定义计划或节点的生命周期状态。
/// Corresponds to Java: io.agentscope.core.plan.PlanStatus
/// 对应 Java: io.agentscope.core.plan.PlanStatus
/// </summary>
public enum PlanStatus
{
    /// <summary>
    /// The plan/node is pending and waiting to be executed.
    /// 计划/节点处于待执行状态。
    /// </summary>
    Pending,

    /// <summary>
    /// The plan/node is currently being executed.
    /// 计划/节点正在执行中。
    /// </summary>
    InProgress,

    /// <summary>
    /// The plan/node has been completed successfully.
    /// 计划/节点已成功完成。
    /// </summary>
    Completed,

    /// <summary>
    /// The plan/node has failed during execution.
    /// 计划/节点执行失败。
    /// </summary>
    Failed,

    /// <summary>
    /// The plan/node has been cancelled before completion.
    /// 计划/节点在完成前被取消。
    /// </summary>
    Cancelled
}

/// <summary>
/// Plan node types enumeration.
/// Plan（计划）节点类型枚举，定义节点在计划树中的角色和行为。
/// Corresponds to Java: io.agentscope.core.plan.PlanNodeType
/// 对应 Java: io.agentscope.core.plan.PlanNodeType
/// </summary>
public enum PlanNodeType
{
    /// <summary>
    /// A concrete task node that can be executed by an agent or tool.
    /// 具体的任务节点，可由 Agent 或工具执行。
    /// </summary>
    Task,

    /// <summary>
    /// A sub-plan node that contains its own child nodes.
    /// 子计划节点，包含自己的子节点。
    /// </summary>
    SubPlan,

    /// <summary>
    /// A decision node that branches execution based on conditions.
    /// 决策节点，根据条件分支执行路径。
    /// </summary>
    Decision,

    /// <summary>
    /// A node whose children are executed in parallel.
    /// 并行节点，其子节点并行执行。
    /// </summary>
    Parallel,

    /// <summary>
    /// A node whose children are executed sequentially in order.
    /// 顺序节点，其子节点按顺序依次执行。
    /// </summary>
    Sequential
}

/// <summary>
/// Represents a single node in the plan tree structure.
/// 表示计划树结构中的单个节点。
/// Each node can be a task, sub-plan, decision point, or a grouping node (parallel/sequential).
/// 每个节点可以是任务、子计划、决策点或分组节点（并行/顺序）。
/// Corresponds to Java: io.agentscope.core.plan.PlanNode
/// 对应 Java: io.agentscope.core.plan.PlanNode
/// </summary>
public class PlanNode
{
    /// <summary>
    /// Unique identifier for the node, auto-generated as a GUID.
    /// 节点的唯一标识符，自动生成为 GUID。
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Node name/title for display and identification purposes.
    /// 节点名称/标题，用于显示和标识。
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Optional detailed description of the node's purpose or task.
    /// 节点目的或任务的详细描述（可选）。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Node type determining its behavior (Task, SubPlan, Decision, Parallel, Sequential).
    /// 节点类型，决定其行为（Task、SubPlan、Decision、Parallel、Sequential）。
    /// </summary>
    public PlanNodeType Type { get; set; } = PlanNodeType.Task;

    /// <summary>
    /// Current execution status of the node.
    /// 节点的当前执行状态。
    /// </summary>
    public PlanStatus Status { get; set; } = PlanStatus.Pending;

    /// <summary>
    /// Parent node ID. Null for root nodes.
    /// 父节点 ID。根节点为 null。
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Child nodes collection. Used for SubPlan, Decision, Parallel, and Sequential types.
    /// 子节点集合。用于 SubPlan、Decision、Parallel 和 Sequential 类型。
    /// </summary>
    public List<PlanNode> Children { get; set; } = new();

    /// <summary>
    /// Dependency node IDs - nodes that must complete before this node can execute.
    /// 依赖节点 ID 列表 - 这些节点必须完成后当前节点才能执行。
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// Input parameters/arguments for the task execution.
    /// 任务执行的输入参数。
    /// </summary>
    public Dictionary<string, object> Inputs { get; set; } = new();

    /// <summary>
    /// Output results produced by the task execution.
    /// 任务执行产生的输出结果。
    /// </summary>
    public Dictionary<string, object> Outputs { get; set; } = new();

    /// <summary>
    /// Execution result message or error description.
    /// 执行结果消息或错误描述。
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Name of the agent assigned to execute this node.
    /// 分配给执行此节点的 Agent 名称。
    /// </summary>
    public string? AssignedAgent { get; set; }

    /// <summary>
    /// Name of the tool to be used for this task.
    /// 此任务要使用的工具名称。
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Maximum execution time in seconds before timeout.
    /// 超时前的最大执行时间（秒）。
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Current retry attempt count (incremented on each retry).
    /// 当前重试次数（每次重试时递增）。
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Maximum number of retry attempts allowed. Default is 3.
    /// 允许的最大重试次数。默认为 3。
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Timestamp when the node was created.
    /// 节点创建时的时间戳。
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the node started execution.
    /// 节点开始执行时的时间戳。
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Timestamp when the node completed execution (success, failure, or cancellation).
    /// 节点完成执行时的时间戳（成功、失败或取消）。
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Extensible metadata dictionary for custom properties.
    /// 可扩展的元数据字典，用于自定义属性。
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Optional hints to guide plan execution (suggested tools, agents, context, etc.).
    /// 可选的执行提示，用于指导计划执行（建议的工具、Agent、上下文等）。
    /// </summary>
    public PlanHints? Hints { get; set; }

    /// <summary>
    /// Checks whether all dependency nodes have been completed.
    /// 检查所有依赖节点是否都已完成。
    /// </summary>
    /// <param name="allNodes">Dictionary of all nodes in the plan, keyed by node ID.
    /// 计划中所有节点的字典，以节点 ID 为键。</param>
    /// <returns>True if all dependencies are satisfied; otherwise false.
    /// 如果所有依赖都已满足则返回 true；否则返回 false。</returns>
    public bool AreDependenciesSatisfied(Dictionary<string, PlanNode> allNodes)
    {
        if (Dependencies.Count == 0) return true;
        
        return Dependencies.All(depId => 
            allNodes.TryGetValue(depId, out var dep) && 
            dep.Status == PlanStatus.Completed);
    }

    /// <summary>
    /// Checks whether this node is ready to be executed (pending and dependencies satisfied).
    /// 检查此节点是否已准备好执行（待执行且依赖已满足）。
    /// </summary>
    /// <param name="allNodes">Dictionary of all nodes in the plan.
    /// 计划中所有节点的字典。</param>
    /// <returns>True if the node can be executed; otherwise false.
    /// 如果节点可以执行则返回 true；否则返回 false。</returns>
    public bool CanExecute(Dictionary<string, PlanNode> allNodes)
    {
        return Status == PlanStatus.Pending && AreDependenciesSatisfied(allNodes);
    }

    /// <summary>
    /// Marks the node as in progress and records the start timestamp.
    /// 将节点标记为进行中，并记录开始时间戳。
    /// </summary>
    public void MarkInProgress()
    {
        Status = PlanStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the node as completed with an optional result message.
    /// 将节点标记为已完成，并附带可选的结果消息。
    /// </summary>
    /// <param name="result">Optional result message from execution.
    /// 执行结果消息（可选）。</param>
    public void MarkCompleted(string? result = null)
    {
        Status = PlanStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Result = result;
    }

    /// <summary>
    /// Marks the node as failed with an error message.
    /// 将节点标记为失败，并附带错误消息。
    /// </summary>
    /// <param name="error">Error message describing the failure.
    /// 描述失败原因的错误消息。</param>
    public void MarkFailed(string error)
    {
        Status = PlanStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        Result = error;
    }

    /// <summary>
    /// Marks the node as cancelled with an optional reason.
    /// 将节点标记为已取消，并附带可选的原因。
    /// </summary>
    /// <param name="reason">Optional cancellation reason. Defaults to "Cancelled".
    /// 取消原因（可选）。默认为 "Cancelled"。</param>
    public void MarkCancelled(string? reason = null)
    {
        Status = PlanStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        Result = reason ?? "Cancelled";
    }

    /// <summary>
    /// Recursively gets all descendant nodes (children, grandchildren, etc.).
    /// 递归获取所有后代节点（子节点、孙节点等）。
    /// </summary>
    /// <returns>An enumerable collection of all descendant nodes.
    /// 所有后代节点的可枚举集合。</returns>
    public IEnumerable<PlanNode> GetAllDescendants()
    {
        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetAllDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Recursively finds a node by its ID within this subtree.
    /// 在此子树中递归查找指定 ID 的节点。
    /// </summary>
    /// <param name="id">The node ID to search for. 要搜索的节点 ID。</param>
    /// <returns>The matching node if found; otherwise null.
    /// 如果找到则返回匹配的节点；否则返回 null。</returns>
    public PlanNode? FindNode(string id)
    {
        if (Id == id) return this;
        
        foreach (var child in Children)
        {
            var found = child.FindNode(id);
            if (found != null) return found;
        }
        
        return null;
    }

    /// <summary>
    /// Calculates the execution progress percentage based on completed nodes.
    /// 根据已完成的节点计算执行进度百分比。
    /// </summary>
    /// <returns>Progress percentage from 0 to 100.
    /// 进度百分比，范围 0 到 100。</returns>
    public double GetProgressPercentage()
    {
        var allNodes = new List<PlanNode> { this };
        allNodes.AddRange(GetAllDescendants());
        
        if (allNodes.Count == 0) return 100;
        
        var completedCount = allNodes.Count(n => n.Status == PlanStatus.Completed);
        return (double)completedCount / allNodes.Count * 100;
    }
}

/// <summary>
/// Represents a complete plan with metadata, status tracking, and execution management.
/// 表示包含元数据、状态跟踪和执行管理的完整计划。
/// Corresponds to Java: io.agentscope.core.plan.Plan
/// 对应 Java: io.agentscope.core.plan.Plan
/// </summary>
public class Plan
{
    /// <summary>
    /// Unique plan identifier, auto-generated as a GUID.
    /// 计划唯一标识符，自动生成为 GUID。
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Plan name for identification and display.
    /// 计划名称，用于标识和显示。
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Optional detailed description of the plan's purpose.
    /// 计划目的的详细描述（可选）。
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Root node of the plan tree. All other nodes are descendants of this node.
    /// 计划树的根节点。所有其他节点都是此节点的后代。
    /// </summary>
    public PlanNode RootNode { get; set; } = new();

    /// <summary>
    /// Overall execution status of the plan.
    /// 计划的整体执行状态。
    /// </summary>
    public PlanStatus Status { get; set; } = PlanStatus.Pending;

    /// <summary>
    /// Name or identifier of the plan creator/owner.
    /// 计划创建者/所有者的名称或标识符。
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the plan was created.
    /// 计划创建时的时间戳。
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of the last update to the plan.
    /// 计划最后更新的时间戳。
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Timestamp when the plan completed execution.
    /// 计划完成执行时的时间戳。
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Global hints applicable to the entire plan.
    /// 适用于整个计划的全局提示。
    /// </summary>
    public PlanHints? GlobalHints { get; set; }

    /// <summary>
    /// Plan-level metadata dictionary for extensibility.
    /// 计划级别的元数据字典，用于扩展。
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Tags for categorization and filtering.
    /// 用于分类和过滤的标签。
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets all nodes in the plan tree as a flat dictionary keyed by node ID.
    /// 获取计划树中所有节点的扁平字典，以节点 ID 为键。
    /// </summary>
    /// <returns>Dictionary mapping node IDs to PlanNode instances.
    /// 将节点 ID 映射到 PlanNode 实例的字典。</returns>
    public Dictionary<string, PlanNode> GetAllNodes()
    {
        var nodes = new Dictionary<string, PlanNode>();
        CollectNodes(RootNode, nodes);
        return nodes;
    }

    /// <summary>
    /// Recursively collects all nodes from the given node into the dictionary.
    /// 递归收集从给定节点开始的所有节点到字典中。
    /// </summary>
    /// <param name="node">Current node to collect. 当前要收集的节点。</param>
    /// <param name="nodes">Dictionary to populate. 要填充的字典。</param>
    private void CollectNodes(PlanNode node, Dictionary<string, PlanNode> nodes)
    {
        nodes[node.Id] = node;
        foreach (var child in node.Children)
        {
            CollectNodes(child, nodes);
        }
    }

    /// <summary>
    /// Finds a node by its ID throughout the entire plan tree.
    /// 在整个计划树中查找指定 ID 的节点。
    /// </summary>
    /// <param name="id">The node ID to search for. 要搜索的节点 ID。</param>
    /// <returns>The matching node if found; otherwise null.
    /// 如果找到则返回匹配的节点；否则返回 null。</returns>
    public PlanNode? FindNode(string id)
    {
        return RootNode.FindNode(id);
    }

    /// <summary>
    /// Gets all nodes that are ready to be executed (pending with satisfied dependencies).
    /// 获取所有已准备好执行的节点（待执行且依赖已满足）。
    /// </summary>
    /// <returns>List of ready-to-execute nodes. 准备就绪可执行的节点列表。</returns>
    public List<PlanNode> GetReadyNodes()
    {
        var allNodes = GetAllNodes();
        return allNodes.Values.Where(n => n.CanExecute(allNodes)).ToList();
    }

    /// <summary>
    /// Gets the overall execution progress percentage of the plan.
    /// 获取计划的整体执行进度百分比。
    /// </summary>
    /// <returns>Progress percentage from 0 to 100.
    /// 进度百分比，范围 0 到 100。</returns>
    public double GetProgressPercentage()
    {
        return RootNode.GetProgressPercentage();
    }

    /// <summary>
    /// Checks whether the plan execution is complete (all nodes in terminal states).
    /// 检查计划执行是否完成（所有节点处于终止状态）。
    /// </summary>
    /// <returns>True if all nodes are completed, failed, or cancelled; otherwise false.
    /// 如果所有节点已完成、失败或取消则返回 true；否则返回 false。</returns>
    public bool IsComplete()
    {
        var allNodes = GetAllNodes().Values;
        return allNodes.All(n => n.Status == PlanStatus.Completed || 
                                  n.Status == PlanStatus.Failed || 
                                  n.Status == PlanStatus.Cancelled);
    }

    /// <summary>
    /// Checks whether the plan executed successfully (all nodes completed).
    /// 检查计划是否成功执行（所有节点已完成）。
    /// </summary>
    /// <returns>True if all nodes are completed; otherwise false.
    /// 如果所有节点已完成则返回 true；否则返回 false。</returns>
    public bool IsSuccessful()
    {
        var allNodes = GetAllNodes().Values;
        return allNodes.All(n => n.Status == PlanStatus.Completed);
    }

    /// <summary>
    /// Generates a summary of the plan execution statistics.
    /// 生成计划执行统计摘要。
    /// </summary>
    /// <returns>A PlanExecutionSummary containing counts and status information.
    /// 包含计数和状态信息的 PlanExecutionSummary。</returns>
    public PlanExecutionSummary GetExecutionSummary()
    {
        var allNodes = GetAllNodes().Values;
        
        return new PlanExecutionSummary
        {
            TotalNodes = allNodes.Count(),
            CompletedNodes = allNodes.Count(n => n.Status == PlanStatus.Completed),
            FailedNodes = allNodes.Count(n => n.Status == PlanStatus.Failed),
            PendingNodes = allNodes.Count(n => n.Status == PlanStatus.Pending),
            InProgressNodes = allNodes.Count(n => n.Status == PlanStatus.InProgress),
            ProgressPercentage = GetProgressPercentage(),
            IsComplete = IsComplete(),
            IsSuccessful = IsSuccessful()
        };
    }
}

/// <summary>
/// Summary statistics for plan execution, providing an overview of completion status.
/// 计划执行摘要统计，提供完成状态的概览。
/// Corresponds to Java: io.agentscope.core.plan.PlanExecutionSummary
/// 对应 Java: io.agentscope.core.plan.PlanExecutionSummary
/// </summary>
public class PlanExecutionSummary
{
    /// <summary>
    /// Total number of nodes in the plan.
    /// 计划中的节点总数。
    /// </summary>
    public int TotalNodes { get; set; }

    /// <summary>
    /// Number of successfully completed nodes.
    /// 成功完成的节点数。
    /// </summary>
    public int CompletedNodes { get; set; }

    /// <summary>
    /// Number of failed nodes.
    /// 失败的节点数。
    /// </summary>
    public int FailedNodes { get; set; }

    /// <summary>
    /// Number of pending (not yet started) nodes.
    /// 待处理（尚未开始）的节点数。
    /// </summary>
    public int PendingNodes { get; set; }

    /// <summary>
    /// Number of nodes currently in progress.
    /// 当前正在执行中的节点数。
    /// </summary>
    public int InProgressNodes { get; set; }

    /// <summary>
    /// Overall progress percentage (0-100).
    /// 总体进度百分比（0-100）。
    /// </summary>
    public double ProgressPercentage { get; set; }

    /// <summary>
    /// Whether the plan execution is complete.
    /// 计划执行是否已完成。
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Whether the plan execution was fully successful.
    /// 计划执行是否完全成功。
    /// </summary>
    public bool IsSuccessful { get; set; }
}

/// <summary>
/// Hints and guidance for plan execution, providing suggestions and constraints.
/// 计划执行的提示和指导，提供建议和约束条件。
/// Corresponds to Java: io.agentscope.core.plan.PlanHints
/// 对应 Java: io.agentscope.core.plan.PlanHints
/// </summary>
public class PlanHints
{
    /// <summary>
    /// List of suggested tool names that could be used for this node.
    /// 可用于此节点的建议工具名称列表。
    /// </summary>
    public List<string> SuggestedTools { get; set; } = new();

    /// <summary>
    /// List of suggested agent names that could execute this node.
    /// 可执行此节点的建议 Agent 名称列表。
    /// </summary>
    public List<string> SuggestedAgents { get; set; } = new();

    /// <summary>
    /// Additional context or instructions for execution.
    /// 执行的额外上下文或指令。
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Example input values to guide execution.
    /// 指导执行的示例输入值。
    /// </summary>
    public List<string> ExampleInputs { get; set; } = new();

    /// <summary>
    /// Example output values to guide expected results.
    /// 指导预期结果的示例输出值。
    /// </summary>
    public List<string> ExampleOutputs { get; set; } = new();

    /// <summary>
    /// Constraints or requirements that must be satisfied.
    /// 必须满足的约束或要求。
    /// </summary>
    public List<string> Constraints { get; set; } = new();

    /// <summary>
    /// Criteria that define successful execution.
    /// 定义成功执行的标准。
    /// </summary>
    public List<string> SuccessCriteria { get; set; } = new();

    /// <summary>
    /// Custom properties dictionary for extensibility.
    /// 用于扩展的自定义属性字典。
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}
