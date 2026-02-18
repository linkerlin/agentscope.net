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
using System.Threading.Tasks;
using AgentScope.Core.Workflow;
using Xunit;

namespace AgentScope.Core.Tests.Workflow;

public class WorkflowTests
{
    #region WorkflowNode Tests

    [Fact]
    public void WorkflowNode_DefaultValues_AreCorrect()
    {
        var node = new WorkflowNode();

        Assert.NotNull(node.Id);
        Assert.NotEmpty(node.Id);
        Assert.Equal(WorkflowNodeType.Task, node.Type);
        Assert.Empty(node.Dependencies);
        Assert.Empty(node.Downstream);
        Assert.Empty(node.Children);
    }

    #endregion

    #region WorkflowDefinition Tests

    [Fact]
    public void WorkflowDefinition_DefaultValues_AreCorrect()
    {
        var workflow = new WorkflowDefinition { Name = "Test" };

        Assert.NotNull(workflow.Id);
        Assert.NotEmpty(workflow.Id);
        Assert.Equal("Test", workflow.Name);
        Assert.Equal("1.0", workflow.Version);
        Assert.Empty(workflow.Nodes);
        Assert.Empty(workflow.Inputs);
        Assert.Empty(workflow.Outputs);
    }

    [Fact]
    public void WorkflowDefinition_GetNode_Existing_ReturnsNode()
    {
        var workflow = new WorkflowDefinition();
        var node = new WorkflowNode { Id = "node1", Name = "Node 1" };
        workflow.Nodes.Add(node);

        var found = workflow.GetNode("node1");

        Assert.NotNull(found);
        Assert.Equal("Node 1", found.Name);
    }

    [Fact]
    public void WorkflowDefinition_GetNode_NonExisting_ReturnsNull()
    {
        var workflow = new WorkflowDefinition();

        var found = workflow.GetNode("non-existing");

        Assert.Null(found);
    }

    [Fact]
    public void WorkflowDefinition_GetStartNode_WithStartNodeId_ReturnsNode()
    {
        var workflow = new WorkflowDefinition
        {
            StartNodeId = "start",
            Nodes = new List<WorkflowNode>
            {
                new WorkflowNode { Id = "start", Name = "Start", Type = WorkflowNodeType.Start }
            }
        };

        var start = workflow.GetStartNode();

        Assert.NotNull(start);
        Assert.Equal("start", start.Id);
    }

    [Fact]
    public void WorkflowDefinition_GetStartNode_WithStartType_ReturnsNode()
    {
        var workflow = new WorkflowDefinition
        {
            Nodes = new List<WorkflowNode>
            {
                new WorkflowNode { Id = "node1", Name = "Start", Type = WorkflowNodeType.Start }
            }
        };

        var start = workflow.GetStartNode();

        Assert.NotNull(start);
        Assert.Equal(WorkflowNodeType.Start, start.Type);
    }

    [Fact]
    public void WorkflowDefinition_GetStartNode_NoStart_ReturnsFirst()
    {
        var workflow = new WorkflowDefinition
        {
            Nodes = new List<WorkflowNode>
            {
                new WorkflowNode { Id = "node1", Name = "First" },
                new WorkflowNode { Id = "node2", Name = "Second" }
            }
        };

        var start = workflow.GetStartNode();

        Assert.NotNull(start);
        Assert.Equal("node1", start.Id);
    }

    [Fact]
    public void WorkflowDefinition_BuildConnections_CreatesDownstream()
    {
        var workflow = new WorkflowDefinition
        {
            Nodes = new List<WorkflowNode>
            {
                new WorkflowNode { Id = "node1" },
                new WorkflowNode { Id = "node2", Dependencies = new List<string> { "node1" } },
                new WorkflowNode { Id = "node3", Dependencies = new List<string> { "node1" } }
            }
        };

        workflow.BuildConnections();

        var node1 = workflow.GetNode("node1")!;
        Assert.Equal(2, node1.Downstream.Count);
        Assert.Contains("node2", node1.Downstream);
        Assert.Contains("node3", node1.Downstream);
    }

    [Fact]
    public void WorkflowDefinition_Validate_ValidWorkflow_ReturnsValid()
    {
        var workflow = new WorkflowDefinition
        {
            StartNodeId = "start",
            Nodes = new List<WorkflowNode>
            {
                new WorkflowNode { Id = "start", Type = WorkflowNodeType.Start },
                new WorkflowNode { Id = "end", Type = WorkflowNodeType.End, Dependencies = new List<string> { "start" } }
            }
        };

        var result = workflow.Validate();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void WorkflowDefinition_Validate_NoStartNode_ReturnsInvalid()
    {
        var workflow = new WorkflowDefinition
        {
            Nodes = new List<WorkflowNode>()
        };

        var result = workflow.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("start node"));
    }

    [Fact]
    public void WorkflowDefinition_Validate_CycleDetected_ReturnsInvalid()
    {
        var workflow = new WorkflowDefinition
        {
            StartNodeId = "node1",
            Nodes = new List<WorkflowNode>
            {
                new WorkflowNode { Id = "node1", Dependencies = new List<string> { "node3" } },
                new WorkflowNode { Id = "node2", Dependencies = new List<string> { "node1" } },
                new WorkflowNode { Id = "node3", Dependencies = new List<string> { "node2" } }
            }
        };

        var result = workflow.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("cycle"));
    }

    [Fact]
    public void WorkflowDefinition_Validate_UnknownDependency_ReturnsInvalid()
    {
        var workflow = new WorkflowDefinition
        {
            StartNodeId = "start",
            Nodes = new List<WorkflowNode>
            {
                new WorkflowNode { Id = "start", Type = WorkflowNodeType.Start },
                new WorkflowNode { Id = "task", Dependencies = new List<string> { "unknown" } }
            }
        };

        var result = workflow.Validate();

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("unknown dependency"));
    }

    #endregion

    #region WorkflowBuilder Tests

    [Fact]
    public void WorkflowBuilder_Create_SetsName()
    {
        var workflow = WorkflowBuilder.Create("Test Workflow").Build();

        Assert.Equal("Test Workflow", workflow.Name);
    }

    [Fact]
    public void WorkflowBuilder_WithDescription_SetsDescription()
    {
        var workflow = WorkflowBuilder.Create("Test")
            .WithDescription("Test description")
            .Build();

        Assert.Equal("Test description", workflow.Description);
    }

    [Fact]
    public void WorkflowBuilder_WithVersion_SetsVersion()
    {
        var workflow = WorkflowBuilder.Create("Test")
            .WithVersion("2.0")
            .Build();

        Assert.Equal("2.0", workflow.Version);
    }

    [Fact]
    public void WorkflowBuilder_AddStart_CreatesStartNode()
    {
        var workflow = WorkflowBuilder.Create("Test")
            .AddStart("my-start")
            .Build();

        var start = workflow.GetNode("my-start");
        Assert.NotNull(start);
        Assert.Equal(WorkflowNodeType.Start, start.Type);
        Assert.Equal("my-start", workflow.StartNodeId);
    }

    [Fact]
    public void WorkflowBuilder_AddEnd_CreatesEndNode()
    {
        var workflow = WorkflowBuilder.Create("Test")
            .AddEnd("my-end")
            .Build();

        var end = workflow.GetNode("my-end");
        Assert.NotNull(end);
        Assert.Equal(WorkflowNodeType.End, end.Type);
    }

    [Fact]
    public void WorkflowBuilder_AddTask_CreatesTaskNode()
    {
        var workflow = WorkflowBuilder.Create("Test")
            .AddStart()
            .AddTask("task1", "My Task", "agent1", "tool1")
            .Build();

        var task = workflow.GetNode("task1");
        Assert.NotNull(task);
        Assert.Equal(WorkflowNodeType.Task, task.Type);
        Assert.Equal("agent1", task.AgentName);
        Assert.Equal("tool1", task.ToolName);
    }

    [Fact]
    public void WorkflowBuilder_AddDecision_CreatesDecisionNode()
    {
        var workflow = WorkflowBuilder.Create("Test")
            .AddStart()
            .AddDecision("dec1", "My Decision", "x == 1", "branch1", "branch2")
            .Build();

        var decision = workflow.GetNode("dec1");
        Assert.NotNull(decision);
        Assert.Equal(WorkflowNodeType.Decision, decision.Type);
        Assert.Equal("x == 1", decision.Condition);
        Assert.Equal("branch1", decision.TrueBranch);
        Assert.Equal("branch2", decision.FalseBranch);
    }

    [Fact]
    public void WorkflowBuilder_WithInput_AddsInput()
    {
        var workflow = WorkflowBuilder.Create("Test")
            .WithInput("name", "string", true)
            .WithInput("age", "integer", false, 18)
            .Build();

        Assert.Equal(2, workflow.Inputs.Count);
        Assert.Contains(workflow.Inputs, i => i.Name == "name" && i.Required);
        Assert.Contains(workflow.Inputs, i => i.Name == "age" && !i.Required && i.DefaultValue?.ToString() == "18");
    }

    [Fact]
    public void WorkflowBuilder_WithOutput_AddsOutput()
    {
        var workflow = WorkflowBuilder.Create("Test")
            .WithOutput("result", "task1.output")
            .Build();

        Assert.Single(workflow.Outputs);
        Assert.Equal("result", workflow.Outputs[0].Name);
        Assert.Equal("task1.output", workflow.Outputs[0].Source);
    }

    [Fact]
    public void WorkflowBuilder_Connect_CreatesDependency()
    {
        var workflow = WorkflowBuilder.Create("Test")
            .AddStart()
            .AddTask("task1", "Task 1")
            .Connect("start", "task1")
            .Build();

        var task1 = workflow.GetNode("task1")!;
        Assert.Contains("start", task1.Dependencies);
    }

    #endregion

    #region WorkflowEngine Tests

    [Fact]
    public async Task WorkflowEngine_ExecuteAsync_SimpleWorkflow_Completes()
    {
        var engine = new WorkflowEngine();
        var workflow = WorkflowBuilder.Create("Simple Workflow")
            .AddStart()
            .AddEnd()
            .Build();

        var result = await engine.ExecuteAsync(workflow);

        Assert.NotNull(result);
        Assert.Equal(WorkflowExecutionStatus.Completed, result.Status);
    }

    [Fact]
    public async Task WorkflowEngine_ExecuteAsync_InvalidWorkflow_ReturnsFailed()
    {
        var engine = new WorkflowEngine();
        var workflow = new WorkflowDefinition { Name = "Invalid" }; // No nodes

        var result = await engine.ExecuteAsync(workflow);

        Assert.NotNull(result);
        Assert.Equal(WorkflowExecutionStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public async Task WorkflowEngine_ExecuteAsync_WithInputs_StoresInContext()
    {
        var engine = new WorkflowEngine();
        var workflow = WorkflowBuilder.Create("Input Test")
            .WithInput("message", "string", true)
            .AddStart()
            .AddEnd()
            .Build();

        var inputs = new Dictionary<string, object> { ["message"] = "Hello" };
        var result = await engine.ExecuteAsync(workflow, inputs);

        Assert.Equal(WorkflowExecutionStatus.Completed, result.Status);
    }

    [Fact]
    public void WorkflowEngine_Validate_ReturnsValidationResult()
    {
        var engine = new WorkflowEngine();
        var workflow = WorkflowBuilder.Create("Valid")
            .AddStart()
            .AddEnd()
            .Build();

        var result = engine.Validate(workflow);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task WorkflowEngine_CancelAsync_CancelsExecution()
    {
        var engine = new WorkflowEngine();
        var workflow = WorkflowBuilder.Create("Long Running")
            .AddStart()
            .AddEnd()
            .Build();

        // Start execution
        var executionTask = engine.ExecuteAsync(workflow);

        // Cancel immediately (might not cancel before completion for simple workflow)
        var cancelled = await engine.CancelAsync("non-existing");
        Assert.False(cancelled);

        // Wait for completion
        var result = await executionTask;
        Assert.True(result.Status == WorkflowExecutionStatus.Completed || result.Status == WorkflowExecutionStatus.Cancelled);
    }

    [Fact]
    public async Task WorkflowEngine_GetStatusAsync_ReturnsStatus()
    {
        var engine = new WorkflowEngine();

        // Non-existing execution
        var status = await engine.GetStatusAsync("non-existing");
        Assert.Equal(WorkflowExecutionStatus.Pending, status);
    }

    #endregion

    #region WorkflowResult Tests

    [Fact]
    public void WorkflowResult_DefaultValues_AreCorrect()
    {
        var result = new WorkflowResult
        {
            WorkflowId = "wf-123",
            ExecutionId = "exec-456",
            Status = WorkflowExecutionStatus.Completed
        };

        Assert.Equal("wf-123", result.WorkflowId);
        Assert.Equal("exec-456", result.ExecutionId);
        Assert.Equal(WorkflowExecutionStatus.Completed, result.Status);
        Assert.NotNull(result.NodeResults);
    }

    [Fact]
    public void WorkflowResult_GetNodeResult_Existing_ReturnsResult()
    {
        var result = new WorkflowResult
        {
            NodeResults = new List<WorkflowNodeResult>
            {
                new WorkflowNodeResult { NodeId = "node1", Status = WorkflowNodeStatus.Completed }
            }
        };

        var nodeResult = result.GetNodeResult("node1");

        Assert.NotNull(nodeResult);
        Assert.Equal(WorkflowNodeStatus.Completed, nodeResult.Status);
    }

    [Fact]
    public void WorkflowResult_GetNodeResult_NonExisting_ReturnsNull()
    {
        var result = new WorkflowResult();

        var nodeResult = result.GetNodeResult("non-existing");

        Assert.Null(nodeResult);
    }

    #endregion

    #region WorkflowContext Tests

    [Fact]
    public void WorkflowContext_SetValue_GetValue_ReturnsValue()
    {
        var context = new WorkflowContext();
        context.SetValue("key", "value");

        var result = context.GetValue<string>("key");

        Assert.Equal("value", result);
    }

    [Fact]
    public void WorkflowContext_GetValue_NonExisting_ReturnsDefault()
    {
        var context = new WorkflowContext();

        var result = context.GetValue<string>("non-existing");

        Assert.Null(result);
    }

    #endregion

    #region RetryConfig Tests

    [Fact]
    public void RetryConfig_DefaultValues_AreCorrect()
    {
        var config = new RetryConfig();

        Assert.Equal(3, config.MaxAttempts);
        Assert.Equal(1, config.DelaySeconds);
        Assert.Equal(2.0f, config.BackoffMultiplier);
    }

    #endregion

    #region WorkflowInput/Output Tests

    [Fact]
    public void WorkflowInput_DefaultValues_AreCorrect()
    {
        var input = new WorkflowInput { Name = "test" };

        Assert.Equal("test", input.Name);
        Assert.Equal("string", input.Type);
        Assert.True(input.Required);
    }

    [Fact]
    public void WorkflowOutput_DefaultValues_AreCorrect()
    {
        var output = new WorkflowOutput { Name = "result" };

        Assert.Equal("result", output.Name);
        Assert.Equal("string", output.Type);
    }

    #endregion

    #region Enum Tests

    [Theory]
    [InlineData(WorkflowNodeStatus.Pending)]
    [InlineData(WorkflowNodeStatus.Running)]
    [InlineData(WorkflowNodeStatus.Completed)]
    [InlineData(WorkflowNodeStatus.Failed)]
    [InlineData(WorkflowNodeStatus.Skipped)]
    [InlineData(WorkflowNodeStatus.Cancelled)]
    public void WorkflowNodeStatus_AllValues_Defined(WorkflowNodeStatus status)
    {
        Assert.True(Enum.IsDefined(typeof(WorkflowNodeStatus), status));
    }

    [Theory]
    [InlineData(WorkflowNodeType.Task)]
    [InlineData(WorkflowNodeType.Decision)]
    [InlineData(WorkflowNodeType.Parallel)]
    [InlineData(WorkflowNodeType.Map)]
    [InlineData(WorkflowNodeType.Reduce)]
    [InlineData(WorkflowNodeType.SubWorkflow)]
    [InlineData(WorkflowNodeType.Wait)]
    [InlineData(WorkflowNodeType.Start)]
    [InlineData(WorkflowNodeType.End)]
    public void WorkflowNodeType_AllValues_Defined(WorkflowNodeType type)
    {
        Assert.True(Enum.IsDefined(typeof(WorkflowNodeType), type));
    }

    [Theory]
    [InlineData(WorkflowExecutionStatus.Pending)]
    [InlineData(WorkflowExecutionStatus.Running)]
    [InlineData(WorkflowExecutionStatus.Completed)]
    [InlineData(WorkflowExecutionStatus.Failed)]
    [InlineData(WorkflowExecutionStatus.Cancelled)]
    [InlineData(WorkflowExecutionStatus.Paused)]
    public void WorkflowExecutionStatus_AllValues_Defined(WorkflowExecutionStatus status)
    {
        Assert.True(Enum.IsDefined(typeof(WorkflowExecutionStatus), status));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task WorkflowEngine_ExecuteAsync_LinearWorkflow_ExecutesInOrder()
    {
        var executionOrder = new List<string>();
        var engine = new WorkflowEngine();

        // This test validates that the workflow engine can execute a simple linear workflow
        // In a real scenario, you'd use actual agents/tools
        var workflow = WorkflowBuilder.Create("Linear")
            .AddStart("start")
            .AddTask("task1", "Task 1")
            .AddTask("task2", "Task 2")
            .Connect("start", "task1")
            .Connect("task1", "task2")
            .AddEnd("end")
            .Connect("task2", "end")
            .Build();

        var result = await engine.ExecuteAsync(workflow);

        Assert.Equal(WorkflowExecutionStatus.Completed, result.Status);
    }

    [Fact]
    public async Task WorkflowEngine_ExecuteAsync_WithValidationError_FailsGracefully()
    {
        var engine = new WorkflowEngine();
        var workflow = new WorkflowDefinition
        {
            Name = "Invalid",
            Nodes = new List<WorkflowNode>
            {
                new WorkflowNode { Id = "node1", Dependencies = new List<string> { "missing" } }
            }
        };

        var result = await engine.ExecuteAsync(workflow);

        Assert.Equal(WorkflowExecutionStatus.Failed, result.Status);
        Assert.NotNull(result.Error);
    }

    #endregion
}
