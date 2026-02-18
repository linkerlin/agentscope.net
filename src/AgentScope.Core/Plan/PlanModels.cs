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
/// Plan 状态枚举
/// </summary>
public enum PlanStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Plan node types.
/// Plan 节点类型
/// </summary>
public enum PlanNodeType
{
    Task,
    SubPlan,
    Decision,
    Parallel,
    Sequential
}

/// <summary>
/// Represents a plan node in the plan tree.
/// Plan 节点模型
/// 
/// Java参考: io.agentscope.core.plan.PlanNode
/// </summary>
public class PlanNode
{
    /// <summary>
    /// Unique identifier for the node.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Node name/title.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Node description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Node type.
    /// </summary>
    public PlanNodeType Type { get; set; } = PlanNodeType.Task;

    /// <summary>
    /// Current status.
    /// </summary>
    public PlanStatus Status { get; set; } = PlanStatus.Pending;

    /// <summary>
    /// Parent node ID (null for root).
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// Child nodes (for SubPlan, Decision, Parallel, Sequential types).
    /// </summary>
    public List<PlanNode> Children { get; set; } = new();

    /// <summary>
    /// Dependencies - IDs of nodes that must complete before this node.
    /// 依赖节点ID列表
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// Input parameters for the task.
    /// </summary>
    public Dictionary<string, object> Inputs { get; set; } = new();

    /// <summary>
    /// Output results from the task.
    /// </summary>
    public Dictionary<string, object> Outputs { get; set; } = new();

    /// <summary>
    /// Execution result/error message.
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Agent assigned to execute this node.
    /// </summary>
    public string? AssignedAgent { get; set; }

    /// <summary>
    /// Tool to be used for this task.
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Maximum execution time in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Maximum retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Start execution timestamp.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Metadata for extensibility.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Hints for plan execution.
    /// </summary>
    public PlanHints? Hints { get; set; }

    /// <summary>
    /// Checks if all dependencies are satisfied.
    /// </summary>
    public bool AreDependenciesSatisfied(Dictionary<string, PlanNode> allNodes)
    {
        if (Dependencies.Count == 0) return true;
        
        return Dependencies.All(depId => 
            allNodes.TryGetValue(depId, out var dep) && 
            dep.Status == PlanStatus.Completed);
    }

    /// <summary>
    /// Checks if this node can be executed.
    /// </summary>
    public bool CanExecute(Dictionary<string, PlanNode> allNodes)
    {
        return Status == PlanStatus.Pending && AreDependenciesSatisfied(allNodes);
    }

    /// <summary>
    /// Marks the node as in progress.
    /// </summary>
    public void MarkInProgress()
    {
        Status = PlanStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the node as completed.
    /// </summary>
    public void MarkCompleted(string? result = null)
    {
        Status = PlanStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Result = result;
    }

    /// <summary>
    /// Marks the node as failed.
    /// </summary>
    public void MarkFailed(string error)
    {
        Status = PlanStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        Result = error;
    }

    /// <summary>
    /// Marks the node as cancelled.
    /// </summary>
    public void MarkCancelled(string? reason = null)
    {
        Status = PlanStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        Result = reason ?? "Cancelled";
    }

    /// <summary>
    /// Gets all descendant nodes.
    /// </summary>
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
    /// Finds a node by ID recursively.
    /// </summary>
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
    /// Gets the execution progress percentage.
    /// </summary>
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
/// Represents a complete plan with metadata.
/// 完整 Plan 模型
/// 
/// Java参考: io.agentscope.core.plan.Plan
/// </summary>
public class Plan
{
    /// <summary>
    /// Unique plan identifier.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Plan name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Plan description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Root node of the plan tree.
    /// </summary>
    public PlanNode RootNode { get; set; } = new();

    /// <summary>
    /// Overall plan status.
    /// </summary>
    public PlanStatus Status { get; set; } = PlanStatus.Pending;

    /// <summary>
    /// Plan creator/owner.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Plan completion timestamp.
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Global plan hints.
    /// </summary>
    public PlanHints? GlobalHints { get; set; }

    /// <summary>
    /// Plan-level metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets all nodes in the plan as a flat dictionary.
    /// </summary>
    public Dictionary<string, PlanNode> GetAllNodes()
    {
        var nodes = new Dictionary<string, PlanNode>();
        CollectNodes(RootNode, nodes);
        return nodes;
    }

    private void CollectNodes(PlanNode node, Dictionary<string, PlanNode> nodes)
    {
        nodes[node.Id] = node;
        foreach (var child in node.Children)
        {
            CollectNodes(child, nodes);
        }
    }

    /// <summary>
    /// Finds a node by ID.
    /// </summary>
    public PlanNode? FindNode(string id)
    {
        return RootNode.FindNode(id);
    }

    /// <summary>
    /// Gets all ready-to-execute nodes.
    /// </summary>
    public List<PlanNode> GetReadyNodes()
    {
        var allNodes = GetAllNodes();
        return allNodes.Values.Where(n => n.CanExecute(allNodes)).ToList();
    }

    /// <summary>
    /// Gets the overall progress percentage.
    /// </summary>
    public double GetProgressPercentage()
    {
        return RootNode.GetProgressPercentage();
    }

    /// <summary>
    /// Checks if the plan is complete.
    /// </summary>
    public bool IsComplete()
    {
        var allNodes = GetAllNodes().Values;
        return allNodes.All(n => n.Status == PlanStatus.Completed || 
                                  n.Status == PlanStatus.Failed || 
                                  n.Status == PlanStatus.Cancelled);
    }

    /// <summary>
    /// Checks if the plan succeeded.
    /// </summary>
    public bool IsSuccessful()
    {
        var allNodes = GetAllNodes().Values;
        return allNodes.All(n => n.Status == PlanStatus.Completed);
    }

    /// <summary>
    /// Gets a summary of the plan execution.
    /// </summary>
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
/// Plan execution summary.
/// Plan 执行摘要
/// </summary>
public class PlanExecutionSummary
{
    public int TotalNodes { get; set; }
    public int CompletedNodes { get; set; }
    public int FailedNodes { get; set; }
    public int PendingNodes { get; set; }
    public int InProgressNodes { get; set; }
    public double ProgressPercentage { get; set; }
    public bool IsComplete { get; set; }
    public bool IsSuccessful { get; set; }
}

/// <summary>
/// Plan hints for guiding execution.
/// Plan 执行提示
/// 
/// Java参考: io.agentscope.core.plan.PlanHints
/// </summary>
public class PlanHints
{
    /// <summary>
    /// Suggested tools for this node.
    /// </summary>
    public List<string> SuggestedTools { get; set; } = new();

    /// <summary>
    /// Suggested agents for this node.
    /// </summary>
    public List<string> SuggestedAgents { get; set; } = new();

    /// <summary>
    /// Additional context/instructions.
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Example inputs.
    /// </summary>
    public List<string> ExampleInputs { get; set; } = new();

    /// <summary>
    /// Example outputs.
    /// </summary>
    public List<string> ExampleOutputs { get; set; } = new();

    /// <summary>
    /// Constraints or requirements.
    /// </summary>
    public List<string> Constraints { get; set; } = new();

    /// <summary>
    /// Success criteria.
    /// </summary>
    public List<string> SuccessCriteria { get; set; } = new();

    /// <summary>
    /// Custom properties.
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();
}
