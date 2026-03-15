// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.State;
using Xunit;

namespace AgentScope.Core.Tests.State;

public class StateTests
{
    [Fact]
    public void StatePersistence_All_HasAllTrue()
    {
        var p = StatePersistence.All;
        Assert.True(p.MemoryManaged);
        Assert.True(p.ToolkitManaged);
        Assert.True(p.PlanNotebookManaged);
    }

    [Fact]
    public void StatePersistence_None_HasAllFalse()
    {
        var p = StatePersistence.None;
        Assert.False(p.MemoryManaged);
        Assert.False(p.ToolkitManaged);
        Assert.False(p.PlanNotebookManaged);
    }

    [Fact]
    public void AgentMetaState_StoresFields()
    {
        var s = new AgentMetaState("id1", "Name", "Desc", "You are helpful.");
        Assert.Equal("id1", s.Id);
        Assert.Equal("Name", s.Name);
        Assert.Equal("You are helpful.", s.SystemPrompt);
    }

    [Fact]
    public void ToolkitState_Empty_HasNoGroups()
    {
        var s = ToolkitState.Empty;
        Assert.Empty(s.ActiveGroups);
    }

    [Fact]
    public void ExampleStateModule_SaveAndLoadIfExists_RoundTrips()
    {
        var session = new AgentScope.Core.Session.Session("sid");
        var module = new ExampleStateModule();
        module.SetMeta(new AgentMetaState("a1", "TestAgent", "Test", "Sys"));
        module.SaveTo(session, "sk");
        var loaded = new ExampleStateModule();
        loaded.LoadIfExists(session, "sk");
        Assert.NotNull(loaded.CurrentMeta);
        Assert.Equal("a1", loaded.CurrentMeta!.Id);
        Assert.Equal("TestAgent", loaded.CurrentMeta.Name);
    }

    [Fact]
    public void ExampleStateModule_LoadFrom_AfterSave_Succeeds()
    {
        var session = new AgentScope.Core.Session.Session("sid");
        var module = new ExampleStateModule();
        module.SetMeta(new AgentMetaState("x", "X", "Y", "Z"));
        module.SaveTo(session, "k");
        var other = new ExampleStateModule();
        other.LoadFrom(session, "k");
        Assert.Equal("x", other.CurrentMeta!.Id);
    }

    [Fact]
    public void ExampleStateModule_LoadIfExists_WhenMissing_DoesNotThrow()
    {
        var session = new AgentScope.Core.Session.Session("sid");
        var module = new ExampleStateModule();
        module.LoadIfExists(session, "nonexistent");
        Assert.Null(module.CurrentMeta);
    }

    /// <summary>
    /// 示例 State 模块：仅持久化 AgentMetaState，用于验收 Save/Load/LoadIfExists。
    /// </summary>
    private sealed class ExampleStateModule : IStateModule
    {
        private const string KeyPrefix = "state::agent_meta::";
        public AgentMetaState? CurrentMeta { get; private set; }

        public void SetMeta(AgentMetaState meta) => CurrentMeta = meta;

        public void SaveTo(AgentScope.Core.Session.Session session, string sessionKey)
        {
            if (CurrentMeta != null)
                session.SetContext(KeyPrefix + sessionKey, CurrentMeta);
        }

        public void LoadFrom(AgentScope.Core.Session.Session session, string sessionKey)
        {
            var v = session.GetContext<AgentMetaState>(KeyPrefix + sessionKey);
            if (v == null)
                throw new InvalidOperationException("State not found: " + sessionKey);
            CurrentMeta = v;
        }

        public void LoadIfExists(AgentScope.Core.Session.Session session, string sessionKey)
        {
            CurrentMeta = session.GetContext<AgentMetaState>(KeyPrefix + sessionKey);
        }
    }
}
