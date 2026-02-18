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
using System.Threading;
using System.Threading.Tasks;
using AgentScope.Core.Agent;
using AgentScope.Core.Message;

namespace AgentScope.Core.Workflow;

/// <summary>
/// Workflow node status.
/// 工作流节点状态
/// </summary>
public enum WorkflowNodeStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Skipped,
    Cancelled
}

/// <summary>
/// Workflow node types.
/// 工作流节点类型
/// </summary>
public enum WorkflowNodeType
{
    /// <summary>
    /// Task node - executes an action.
    /// </summary>
    Task,

    /// <summary>
    /// Decision node - conditional branch.
    /// </summary>
    Decision,

    /// <summary>
    /// Parallel node - executes children in parallel.
    /// </summary>
    Parallel,

    /// <summary>
    /// Map node - applies operation to each item.
    /// </summary>
    Map,

    /// <summary>
    /// Reduce node - aggregates results.
    /// </summary>
    Reduce,

    /// <summary>
    /// Sub-workflow node - nested workflow.
    /// </summary>
    SubWorkflow,

    /// <summary>
    /// Wait node - waits for external signal.
    /// </summary>
    Wait,

    /// <summary>
    /// Start node - entry point.
    /// </summary>
    Start,

    /// <summary>
    /// End node - exit point.
    /// </summary>
    End
}

/// <summary>
/// Workflow execution status.
/// 工作流执行状态
/// </summary>
public enum WorkflowExecutionStatus
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Paused
}

/// <summary>
/// Workflow node definition.
/// 工作流节点定义
/// </summary>
public class WorkflowNode
{
    /// <summary>
    /// Node ID (unique within workflow).
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Node name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Node description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Node type.
    /// </summary>
    public WorkflowNodeType Type { get; set; } = WorkflowNodeType.Task;

    /// <summary>
    /// Agent to execute this node (for Task type).
    /// </summary>
    public string? AgentName { get; set; }

    /// <summary>
    /// Tool to execute (for Task type).
    /// </summary>
    public string? ToolName { get; set; }

    /// <summary>
    /// Input parameters.
    /// </summary>
    public Dictionary<string, object> Inputs { get; set; } = new();

    /// <summary>
    /// Output mappings.
    /// </summary>
    public Dictionary<string, string> Outputs { get; set; } = new();

    /// <summary>
    /// Upstream dependencies (node IDs that must complete before this node).
    /// </summary>
    public List<string> Dependencies { get; set; } = new();

    /// <summary>
    /// Downstream nodes (auto-populated).
    /// </summary>
    public List<string> Downstream { get; set; } = new();

    /// <summary>
    /// Condition expression (for Decision type).
    /// </summary>
    public string? Condition { get; set; }

    /// <summary>
    /// True branch node ID (for Decision type).
    /// </summary>
    public string? TrueBranch { get; set; }

    /// <summary>
    /// False branch node ID (for Decision type).
    /// </summary>
    public string? FalseBranch { get; set; }

    /// <summary>
    /// Child nodes (for Parallel/Map/SubWorkflow types).
    /// </summary>
    public List<WorkflowNode> Children { get; set; } = new();

    /// <summary>
    /// Retry configuration.
    /// </summary>
    public RetryConfig? Retry { get; set; }

    /// <summary>
    /// Timeout in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Custom node configuration.
    /// </summary>
    public Dictionary<string, object> Config { get; set; } = new();

    /// <summary>
    /// Checks if this is a start node.
    /// </summary>
    public bool IsStart => Type == WorkflowNodeType.Start;

    /// <summary>
    /// Checks if this is an end node.
    /// </summary>
    public bool IsEnd => Type == WorkflowNodeType.End;
}

/// <summary>
/// Retry configuration.
/// </summary>
public class RetryConfig
{
    /// <summary>
    /// Maximum retry attempts.
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Delay between retries in seconds.
    /// </summary>
    public int DelaySeconds { get; set; } = 1;

    /// <summary>
    /// Exponential backoff multiplier.
    /// </summary>
    public float BackoffMultiplier { get; set; } = 2.0f;
}

/// <summary>
/// Workflow definition.
/// 工作流定义
/// </summary>
public class WorkflowDefinition
{
    /// <summary>
    /// Workflow ID.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Workflow name.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Workflow description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Start node ID.
    /// </summary>
    public string? StartNodeId { get; set; }

    /// <summary>
    /// All nodes in the workflow.
    /// </summary>
    public List<WorkflowNode> Nodes { get; set; } = new();

    /// <summary>
    /// Global inputs.
    /// </summary>
    public List<WorkflowInput> Inputs { get; set; } = new();

    /// <summary>
    /// Global outputs.
    /// </summary>
    public List<WorkflowOutput> Outputs { get; set; } = new();

    /// <summary>
    /// Workflow-level variables.
    /// </summary>
    public Dictionary<string, object> Variables { get; set; } = new();

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Tags.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets a node by ID.
    /// </summary>
    public WorkflowNode? GetNode(string id)
    {
        return Nodes.FirstOrDefault(n => n.Id == id);
    }

    /// <summary>
    /// Gets the start node.
    /// </summary>
    public WorkflowNode? GetStartNode()
    {
        if (!string.IsNullOrEmpty(StartNodeId))
        {
            return GetNode(StartNodeId);
        }
        return Nodes.FirstOrDefault(n => n.IsStart) ?? Nodes.FirstOrDefault();
    }

    /// <summary>
    /// Builds the downstream connections.
    /// </summary>
    public void BuildConnections()
    {
        // Clear existing downstream
        foreach (var node in Nodes)
        {
            node.Downstream.Clear();
        }

        // Build downstream from dependencies
        foreach (var node in Nodes)
        {
            foreach (var depId in node.Dependencies)
            {
                var dep = GetNode(depId);
                if (dep != null && !dep.Downstream.Contains(node.Id))
                {
                    dep.Downstream.Add(node.Id);
                }
            }
        }
    }

    /// <summary>
    /// Validates the workflow definition.
    /// </summary>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        // Check start node
        var startNode = GetStartNode();
        if (startNode == null)
        {
            errors.Add("Workflow must have a start node");
        }

        // Check for cycles using DFS
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        bool HasCycle(string nodeId)
        {
            visited.Add(nodeId);
            recursionStack.Add(nodeId);

            var node = GetNode(nodeId);
            if (node != null)
            {
                foreach (var downstream in node.Downstream)
                {
                    if (!visited.Contains(downstream))
                    {
                        if (HasCycle(downstream))
                            return true;
                    }
                    else if (recursionStack.Contains(downstream))
                    {
                        return true;
                    }
                }
            }

            recursionStack.Remove(nodeId);
            return false;
        }

        BuildConnections();

        if (startNode != null && HasCycle(startNode.Id))
        {
            errors.Add("Workflow contains a cycle");
        }

        // Check all dependencies exist
        foreach (var node in Nodes)
        {
            foreach (var depId in node.Dependencies)
            {
                if (GetNode(depId) == null)
                {
                    errors.Add($"Node '{node.Id}' has unknown dependency '{depId}'");
                }
            }
        }

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}

/// <summary>
/// Workflow input definition.
/// </summary>
public class WorkflowInput
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "string";
    public object? DefaultValue { get; set; }
    public bool Required { get; set; } = true;
}

/// <summary>
/// Workflow output definition.
/// </summary>
public class WorkflowOutput
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string Type { get; set; } = "string";
    public string? Source { get; set; } // Node output mapping
}

/// <summary>
/// Validation result.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Workflow node execution result.
/// </summary>
public class WorkflowNodeResult
{
    public string NodeId { get; set; } = "";
    public WorkflowNodeStatus Status { get; set; }
    public Dictionary<string, object> Outputs { get; set; } = new();
    public string? Error { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int AttemptCount { get; set; }
}

/// <summary>
/// Workflow execution result.
/// </summary>
public class WorkflowResult
{
    public string WorkflowId { get; set; } = "";
    public string ExecutionId { get; set; } = "";
    public WorkflowExecutionStatus Status { get; set; }
    public Dictionary<string, object> Outputs { get; set; } = new();
    public List<WorkflowNodeResult> NodeResults { get; set; } = new();
    public string? Error { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration => CompletedAt - StartedAt;

    /// <summary>
    /// Gets a specific node result.
    /// </summary>
    public WorkflowNodeResult? GetNodeResult(string nodeId)
    {
        return NodeResults.FirstOrDefault(r => r.NodeId == nodeId);
    }
}

/// <summary>
/// Workflow execution context.
/// </summary>
public class WorkflowContext
{
    /// <summary>
    /// Execution ID.
    /// </summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Workflow definition.
    /// </summary>
    public WorkflowDefinition Workflow { get; set; } = new();

    /// <summary>
    /// Input parameters.
    /// </summary>
    public Dictionary<string, object> Inputs { get; set; } = new();

    /// <summary>
    /// Execution state (shared across nodes).
    /// </summary>
    public Dictionary<string, object> State { get; set; } = new();

    /// <summary>
    /// Node execution results.
    /// </summary>
    public Dictionary<string, WorkflowNodeResult> Results { get; set; } = new();

    /// <summary>
    /// Available agents.
    /// </summary>
    public Dictionary<string, IAgent> Agents { get; set; } = new();

    /// <summary>
    /// Cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; set; }

    /// <summary>
    /// Gets a value from state.
    /// </summary>
    public T? GetValue<T>(string key)
    {
        if (State.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    /// <summary>
    /// Sets a value in state.
    /// </summary>
    public void SetValue<T>(string key, T value)
    {
        State[key] = value!;
    }
}

/// <summary>
/// Interface for workflow engine.
/// 工作流引擎接口
/// </summary>
public interface IWorkflowEngine
{
    /// <summary>
    /// Executes a workflow.
    /// </summary>
    Task<WorkflowResult> ExecuteAsync(WorkflowDefinition workflow, Dictionary<string, object>? inputs = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a workflow definition.
    /// </summary>
    ValidationResult Validate(WorkflowDefinition workflow);

    /// <summary>
    /// Gets execution status.
    /// </summary>
    Task<WorkflowExecutionStatus> GetStatusAsync(string executionId);

    /// <summary>
    /// Cancels a running workflow.
    /// </summary>
    Task<bool> CancelAsync(string executionId);
}
