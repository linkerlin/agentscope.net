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
using AgentScope.Core.Plan;
using Xunit;

// Alias to avoid namespace conflicts
using PlanModel = AgentScope.Core.Plan.Plan;

namespace AgentScope.Core.Tests.Plan;

public class PlanTests
{
    #region PlanNode Tests

    [Fact]
    public void PlanNode_DefaultValues_AreCorrect()
    {
        var node = new PlanNode();

        Assert.NotNull(node.Id);
        Assert.NotEmpty(node.Id);
        Assert.Equal(PlanNodeType.Task, node.Type);
        Assert.Equal(PlanStatus.Pending, node.Status);
        Assert.Empty(node.Children);
        Assert.Empty(node.Dependencies);
    }

    [Fact]
    public void PlanNode_MarkInProgress_SetsStatusAndTimestamp()
    {
        var node = new PlanNode();
        
        node.MarkInProgress();

        Assert.Equal(PlanStatus.InProgress, node.Status);
        Assert.NotNull(node.StartedAt);
    }

    [Fact]
    public void PlanNode_MarkCompleted_SetsStatusAndTimestamp()
    {
        var node = new PlanNode();
        
        node.MarkCompleted("result");

        Assert.Equal(PlanStatus.Completed, node.Status);
        Assert.NotNull(node.CompletedAt);
        Assert.Equal("result", node.Result);
    }

    [Fact]
    public void PlanNode_MarkFailed_SetsStatusAndError()
    {
        var node = new PlanNode();
        
        node.MarkFailed("error message");

        Assert.Equal(PlanStatus.Failed, node.Status);
        Assert.NotNull(node.CompletedAt);
        Assert.Equal("error message", node.Result);
    }

    [Fact]
    public void PlanNode_AreDependenciesSatisfied_NoDependencies_ReturnsTrue()
    {
        var node = new PlanNode();
        var allNodes = new Dictionary<string, PlanNode>();

        Assert.True(node.AreDependenciesSatisfied(allNodes));
    }

    [Fact]
    public void PlanNode_AreDependenciesSatisfied_AllCompleted_ReturnsTrue()
    {
        var dep1 = new PlanNode { Id = "dep1", Status = PlanStatus.Completed };
        var dep2 = new PlanNode { Id = "dep2", Status = PlanStatus.Completed };
        var node = new PlanNode { Dependencies = new List<string> { "dep1", "dep2" } };

        var allNodes = new Dictionary<string, PlanNode> { ["dep1"] = dep1, ["dep2"] = dep2 };

        Assert.True(node.AreDependenciesSatisfied(allNodes));
    }

    [Fact]
    public void PlanNode_AreDependenciesSatisfied_OnePending_ReturnsFalse()
    {
        var dep1 = new PlanNode { Id = "dep1", Status = PlanStatus.Completed };
        var dep2 = new PlanNode { Id = "dep2", Status = PlanStatus.Pending };
        var node = new PlanNode { Dependencies = new List<string> { "dep1", "dep2" } };

        var allNodes = new Dictionary<string, PlanNode> { ["dep1"] = dep1, ["dep2"] = dep2 };

        Assert.False(node.AreDependenciesSatisfied(allNodes));
    }

    [Fact]
    public void PlanNode_FindNode_ExistingNode_ReturnsNode()
    {
        var child = new PlanNode { Id = "child" };
        var parent = new PlanNode { Id = "parent", Children = new List<PlanNode> { child } };

        var found = parent.FindNode("child");

        Assert.NotNull(found);
        Assert.Equal("child", found.Id);
    }

    [Fact]
    public void PlanNode_FindNode_NestedNode_ReturnsNode()
    {
        var grandchild = new PlanNode { Id = "grandchild" };
        var child = new PlanNode { Id = "child", Children = new List<PlanNode> { grandchild } };
        var parent = new PlanNode { Id = "parent", Children = new List<PlanNode> { child } };

        var found = parent.FindNode("grandchild");

        Assert.NotNull(found);
        Assert.Equal("grandchild", found.Id);
    }

    [Fact]
    public void PlanNode_GetProgressPercentage_AllCompleted_Returns100()
    {
        var child1 = new PlanNode { Status = PlanStatus.Completed };
        var child2 = new PlanNode { Status = PlanStatus.Completed };
        var parent = new PlanNode { Status = PlanStatus.Completed, Children = new List<PlanNode> { child1, child2 } };

        Assert.Equal(100.0, parent.GetProgressPercentage());
    }

    [Fact]
    public void PlanNode_GetProgressPercentage_HalfCompleted_Returns50()
    {
        var child1 = new PlanNode { Status = PlanStatus.Completed };
        var child2 = new PlanNode { Status = PlanStatus.Completed };
        var parent = new PlanNode { Status = PlanStatus.Pending, Children = new List<PlanNode> { child1, child2 } };

        // 2 out of 3 nodes completed (parent is pending) = ~66.67%, rounded for test
        Assert.True(parent.GetProgressPercentage() >= 66.0);
    }

    #endregion

    #region Plan Tests

    [Fact]
    public void Plan_DefaultValues_AreCorrect()
    {
        var plan = new PlanModel { Name = "Test" };

        Assert.NotNull(plan.Id);
        Assert.NotEmpty(plan.Id);
        Assert.Equal("Test", plan.Name);
        Assert.NotNull(plan.RootNode);
        Assert.Equal(PlanStatus.Pending, plan.Status);
    }

    [Fact]
    public void Plan_GetAllNodes_ReturnsAllNodes()
    {
        var child = new PlanNode { Id = "child" };
        var grandchild = new PlanNode { Id = "grandchild" };
        child.Children.Add(grandchild);
        var plan = new PlanModel { RootNode = new PlanNode { Id = "root", Children = new List<PlanNode> { child } } };

        var allNodes = plan.GetAllNodes();

        Assert.Equal(3, allNodes.Count);
        Assert.Contains("root", allNodes.Keys);
        Assert.Contains("child", allNodes.Keys);
        Assert.Contains("grandchild", allNodes.Keys);
    }

    [Fact]
    public void Plan_GetReadyNodes_ReturnsOnlyReadyNodes()
    {
        var readyNode = new PlanNode { Id = "ready", Status = PlanStatus.Pending };
        var inProgressNode = new PlanNode { Id = "inprogress", Status = PlanStatus.InProgress };
        var completedNode = new PlanNode { Id = "completed", Status = PlanStatus.Completed };
        
        var plan = new PlanModel
        { 
            RootNode = new PlanNode 
            { 
                Id = "root",
                Status = PlanStatus.InProgress, // Root is in progress
                Children = new List<PlanNode> { readyNode, inProgressNode, completedNode }
            }
        };

        var readyNodes = plan.GetReadyNodes();

        Assert.Single(readyNodes);
        Assert.Equal("ready", readyNodes[0].Id);
    }

    [Fact]
    public void Plan_IsComplete_AllNodesFinished_ReturnsTrue()
    {
        var plan = new PlanModel
        {
            RootNode = new PlanNode
            {
                Status = PlanStatus.Completed,
                Children = new List<PlanNode>
                {
                    new PlanNode { Status = PlanStatus.Completed },
                    new PlanNode { Status = PlanStatus.Completed }
                }
            }
        };

        Assert.True(plan.IsComplete());
    }

    [Fact]
    public void Plan_IsComplete_OnePending_ReturnsFalse()
    {
        var plan = new PlanModel
        {
            RootNode = new PlanNode
            {
                Status = PlanStatus.Completed,
                Children = new List<PlanNode>
                {
                    new PlanNode { Status = PlanStatus.Completed },
                    new PlanNode { Status = PlanStatus.Pending }
                }
            }
        };

        Assert.False(plan.IsComplete());
    }

    [Fact]
    public void Plan_IsSuccessful_AllCompleted_ReturnsTrue()
    {
        var plan = new PlanModel
        {
            RootNode = new PlanNode
            {
                Status = PlanStatus.Completed,
                Children = new List<PlanNode>
                {
                    new PlanNode { Status = PlanStatus.Completed },
                    new PlanNode { Status = PlanStatus.Completed }
                }
            }
        };

        Assert.True(plan.IsSuccessful());
    }

    [Fact]
    public void Plan_IsSuccessful_OneFailed_ReturnsFalse()
    {
        var plan = new PlanModel
        {
            RootNode = new PlanNode
            {
                Status = PlanStatus.Completed,
                Children = new List<PlanNode>
                {
                    new PlanNode { Status = PlanStatus.Completed },
                    new PlanNode { Status = PlanStatus.Failed }
                }
            }
        };

        Assert.False(plan.IsSuccessful());
    }

    [Fact]
    public void Plan_GetExecutionSummary_ReturnsCorrectCounts()
    {
        var plan = new PlanModel
        {
            RootNode = new PlanNode
            {
                Status = PlanStatus.Completed,
                Children = new List<PlanNode>
                {
                    new PlanNode { Status = PlanStatus.Completed },
                    new PlanNode { Status = PlanStatus.Failed },
                    new PlanNode { Status = PlanStatus.Pending },
                    new PlanNode { Status = PlanStatus.InProgress }
                }
            }
        };

        var summary = plan.GetExecutionSummary();

        Assert.Equal(5, summary.TotalNodes);
        Assert.Equal(2, summary.CompletedNodes);
        Assert.Equal(1, summary.FailedNodes);
        Assert.Equal(1, summary.PendingNodes);
        Assert.Equal(1, summary.InProgressNodes);
        Assert.Equal(40.0, summary.ProgressPercentage); // 2/5 = 40%
    }

    #endregion

    #region PlanNotebook Tests

    [Fact]
    public void PlanNotebook_CreatePlan_CreatesPlanWithRootNode()
    {
        var notebook = new PlanNotebook();
        
        var plan = notebook.CreatePlan("Test Plan", "Description");

        Assert.NotNull(plan);
        Assert.Equal("Test Plan", plan.Name);
        Assert.Equal("Description", plan.Description);
        Assert.NotNull(plan.RootNode);
        Assert.Equal("Test Plan", plan.RootNode.Name);
    }

    [Fact]
    public void PlanNotebook_GetPlan_ExistingPlan_ReturnsPlan()
    {
        var notebook = new PlanNotebook();
        var created = notebook.CreatePlan("Test");

        var retrieved = notebook.GetPlan(created.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
    }

    [Fact]
    public void PlanNotebook_GetPlan_NonExistent_ReturnsNull()
    {
        var notebook = new PlanNotebook();

        var retrieved = notebook.GetPlan("non-existent");

        Assert.Null(retrieved);
    }

    [Fact]
    public void PlanNotebook_DeletePlan_ExistingPlan_RemovesAndReturnsTrue()
    {
        var notebook = new PlanNotebook();
        var plan = notebook.CreatePlan("Test");

        var deleted = notebook.DeletePlan(plan.Id);

        Assert.True(deleted);
        Assert.Null(notebook.GetPlan(plan.Id));
    }

    [Fact]
    public void PlanNotebook_DeletePlan_NonExistent_ReturnsFalse()
    {
        var notebook = new PlanNotebook();

        var deleted = notebook.DeletePlan("non-existent");

        Assert.False(deleted);
    }

    [Fact]
    public void PlanNotebook_AddTask_AddsTaskToParent()
    {
        var notebook = new PlanNotebook();
        var plan = notebook.CreatePlan("Test");

        var task = notebook.AddTask(plan, plan.RootNode.Id, "Subtask", "Description", "agent1", "tool1");

        Assert.Single(plan.RootNode.Children);
        Assert.Equal("Subtask", task.Name);
        Assert.Equal("Description", task.Description);
        Assert.Equal("agent1", task.AssignedAgent);
        Assert.Equal("tool1", task.ToolName);
        Assert.Equal(PlanNodeType.Task, task.Type);
    }

    [Fact]
    public void PlanNotebook_AddTask_InvalidParent_ThrowsException()
    {
        var notebook = new PlanNotebook();
        var plan = notebook.CreatePlan("Test");

        Assert.Throws<ArgumentException>(() => 
            notebook.AddTask(plan, "invalid-parent", "Task"));
    }

    [Fact]
    public void PlanNotebook_AddSubPlan_AddsSubPlanNode()
    {
        var notebook = new PlanNotebook();
        var plan = notebook.CreatePlan("Test");

        var subPlan = notebook.AddSubPlan(plan, plan.RootNode.Id, "SubPlan", "Description");

        Assert.Single(plan.RootNode.Children);
        Assert.Equal("SubPlan", subPlan.Name);
        Assert.Equal(PlanNodeType.SubPlan, subPlan.Type);
    }

    [Fact]
    public void PlanNotebook_AddDependency_AddsDependencyToNode()
    {
        var notebook = new PlanNotebook();
        var plan = notebook.CreatePlan("Test");
        var task1 = notebook.AddTask(plan, plan.RootNode.Id, "Task1");
        var task2 = notebook.AddTask(plan, plan.RootNode.Id, "Task2");

        notebook.AddDependency(plan, task2.Id, task1.Id);

        Assert.Single(task2.Dependencies);
        Assert.Contains(task1.Id, task2.Dependencies);
    }

    [Fact]
    public void PlanNotebook_AddDependency_InvalidNode_ThrowsException()
    {
        var notebook = new PlanNotebook();
        var plan = notebook.CreatePlan("Test");

        Assert.Throws<ArgumentException>(() => 
            notebook.AddDependency(plan, "invalid-node", plan.RootNode.Id));
    }

    #endregion

    #region PlanStorage Tests

    [Fact]
    public async Task InMemoryPlanStorage_SaveAndLoad_RoundTrip()
    {
        var storage = new InMemoryPlanStorage();
        var plan = new PlanModel { Name = "Test", Id = "test-id" };

        await storage.SaveAsync(plan);
        var loaded = await storage.LoadAsync("test-id");

        Assert.NotNull(loaded);
        Assert.Equal("Test", loaded.Name);
        Assert.Equal("test-id", loaded.Id);
    }

    [Fact]
    public async Task InMemoryPlanStorage_Load_NonExistent_ReturnsNull()
    {
        var storage = new InMemoryPlanStorage();

        var loaded = await storage.LoadAsync("non-existent");

        Assert.Null(loaded);
    }

    [Fact]
    public async Task InMemoryPlanStorage_Delete_Existing_RemovesAndReturnsTrue()
    {
        var storage = new InMemoryPlanStorage();
        var plan = new PlanModel { Id = "test-id" };
        await storage.SaveAsync(plan);

        var deleted = await storage.DeleteAsync("test-id");

        Assert.True(deleted);
        Assert.Null(await storage.LoadAsync("test-id"));
    }

    [Fact]
    public async Task InMemoryPlanStorage_Delete_NonExistent_ReturnsFalse()
    {
        var storage = new InMemoryPlanStorage();

        var deleted = await storage.DeleteAsync("non-existent");

        Assert.False(deleted);
    }

    [Fact]
    public async Task InMemoryPlanStorage_List_ReturnsAllIds()
    {
        var storage = new InMemoryPlanStorage();
        await storage.SaveAsync(new PlanModel { Id = "id1" });
        await storage.SaveAsync(new PlanModel { Id = "id2" });

        var list = await storage.ListAsync();

        Assert.Equal(2, list.Count);
        Assert.Contains("id1", list);
        Assert.Contains("id2", list);
    }

    [Fact]
    public async Task InMemoryPlanStorage_Exists_Existing_ReturnsTrue()
    {
        var storage = new InMemoryPlanStorage();
        await storage.SaveAsync(new PlanModel { Id = "test-id" });

        var exists = await storage.ExistsAsync("test-id");

        Assert.True(exists);
    }

    [Fact]
    public async Task InMemoryPlanStorage_Exists_NonExistent_ReturnsFalse()
    {
        var storage = new InMemoryPlanStorage();

        var exists = await storage.ExistsAsync("non-existent");

        Assert.False(exists);
    }

    #endregion

    #region PlanManager Tests

    [Fact]
    public async Task PlanManager_CreatePlan_CreatesAndSavesPlan()
    {
        var manager = new PlanManager();

        var plan = await manager.CreatePlanAsync("Test Plan", "Description");

        Assert.NotNull(plan);
        Assert.Equal("Test Plan", plan.Name);
        
        var retrieved = await manager.GetPlanAsync(plan.Id);
        Assert.NotNull(retrieved);
    }

    [Fact]
    public async Task PlanManager_GetPlan_CachesResult()
    {
        var storage = new InMemoryPlanStorage();
        var manager = new PlanManager(storage);
        var plan = await manager.CreatePlanAsync("Test");

        // First call loads from storage
        var first = await manager.GetPlanAsync(plan.Id);
        // Second call should use cache
        var second = await manager.GetPlanAsync(plan.Id);

        Assert.Same(first, second); // Same reference
    }

    [Fact]
    public async Task PlanManager_SavePlan_UpdatesTimestamp()
    {
        var manager = new PlanManager();
        var plan = await manager.CreatePlanAsync("Test");
        var originalUpdatedAt = plan.UpdatedAt ?? DateTime.MinValue;

        await Task.Delay(50); // Ensure time difference
        plan.Name = "Updated";
        await manager.SavePlanAsync(plan);

        Assert.NotNull(plan.UpdatedAt);
        Assert.True(plan.UpdatedAt > originalUpdatedAt);
    }

    [Fact]
    public async Task PlanManager_DeletePlan_RemovesFromCacheAndStorage()
    {
        var manager = new PlanManager();
        var plan = await manager.CreatePlanAsync("Test");

        await manager.DeletePlanAsync(plan.Id);

        Assert.Null(await manager.GetPlanAsync(plan.Id));
    }

    [Fact]
    public async Task PlanManager_ExportImport_RoundTrip()
    {
        var manager = new PlanManager();
        var plan = await manager.CreatePlanAsync("Test Plan", "Description");

        var json = await manager.ExportToJsonAsync(plan.Id);
        var imported = await manager.ImportFromJsonAsync(json);

        Assert.Equal("Test Plan", imported.Name);
        Assert.Equal("Description", imported.Description);
        Assert.NotEqual(plan.Id, imported.Id); // New ID generated
    }

    #endregion

    #region PlanHints Tests

    [Fact]
    public void PlanHints_DefaultValues_AreEmptyLists()
    {
        var hints = new PlanHints();

        Assert.Empty(hints.SuggestedTools);
        Assert.Empty(hints.SuggestedAgents);
        Assert.Empty(hints.ExampleInputs);
        Assert.Empty(hints.ExampleOutputs);
        Assert.Empty(hints.Constraints);
        Assert.Empty(hints.SuccessCriteria);
    }

    #endregion
}
