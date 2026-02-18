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

using Xunit;
using AgentScope.Core.Session;
using System;
using System.Threading.Tasks;

namespace AgentScope.Core.Tests.Session;

public class SessionTests
{
    [Fact]
    public void Session_DefaultConstructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var session = new Core.Session.Session();

        // Assert
        Assert.NotNull(session.Id);
        Assert.NotEmpty(session.Id);
        Assert.NotNull(session.Name);
        Assert.True(session.CreatedAt <= DateTime.UtcNow);
        Assert.True(session.UpdatedAt <= DateTime.UtcNow);
        Assert.Equal(SessionStatus.Active, session.Status);
        Assert.NotNull(session.Metadata);
        Assert.NotNull(session.Context);
    }

    [Fact]
    public void Session_WithCustomName_ShouldUseProvidedName()
    {
        // Arrange
        var name = "Test Session";

        // Act
        var session = new Core.Session.Session(name: name);

        // Assert
        Assert.Equal(name, session.Name);
    }

    [Fact]
    public void Session_WithCustomId_ShouldUseProvidedId()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();

        // Act
        var session = new Core.Session.Session(id: id);

        // Assert
        Assert.Equal(id, session.Id);
    }

    [Fact]
    public void Session_Touch_ShouldUpdateTimestamp()
    {
        // Arrange
        var session = new Core.Session.Session();
        var originalTime = session.UpdatedAt;
        
        // Wait a bit to ensure time difference
        System.Threading.Thread.Sleep(10);

        // Act
        session.Touch();

        // Assert
        Assert.True(session.UpdatedAt > originalTime);
    }

    [Fact]
    public void Session_SetAndGetContext_ShouldWorkCorrectly()
    {
        // Arrange
        var session = new Core.Session.Session();
        var key = "test_key";
        var value = "test_value";

        // Act
        session.SetContext(key, value);
        var retrieved = session.GetContext<string>(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public void Session_GetContext_WithWrongType_ShouldReturnDefault()
    {
        // Arrange
        var session = new Core.Session.Session();
        session.SetContext("key", "string_value");

        // Act
        var result = session.GetContext<int>("key");

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public void Session_SetAndGetMetadata_ShouldWorkCorrectly()
    {
        // Arrange
        var session = new Core.Session.Session();
        var key = "version";
        var value = 1;

        // Act
        session.SetMetadata(key, value);
        var retrieved = session.GetMetadata<int>(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public void Session_SetContext_ShouldTouchSession()
    {
        // Arrange
        var session = new Core.Session.Session();
        var originalTime = session.UpdatedAt;
        System.Threading.Thread.Sleep(10);

        // Act
        session.SetContext("key", "value");

        // Assert
        Assert.True(session.UpdatedAt > originalTime);
    }

    [Fact]
    public void Session_AgentName_ShouldBeSettable()
    {
        // Arrange
        var session = new Core.Session.Session();
        var agentName = "TestAgent";

        // Act
        session.AgentName = agentName;

        // Assert
        Assert.Equal(agentName, session.AgentName);
    }

    [Fact]
    public void Session_Status_ShouldBeChangeable()
    {
        // Arrange
        var session = new Core.Session.Session();

        // Act
        session.Status = SessionStatus.Paused;

        // Assert
        Assert.Equal(SessionStatus.Paused, session.Status);
    }
}

public class SessionManagerTests
{
    [Fact]
    public void SessionManager_CreateSession_ShouldReturnNewSession()
    {
        // Arrange
        var manager = new SessionManager();

        // Act
        var session = manager.CreateSession();

        // Assert
        Assert.NotNull(session);
        Assert.Equal(1, manager.SessionCount);
        Assert.Equal(session, manager.CurrentSession);
    }

    [Fact]
    public void SessionManager_CreateSession_WithName_ShouldUseProvidedName()
    {
        // Arrange
        var manager = new SessionManager();
        var name = "Custom Session";

        // Act
        var session = manager.CreateSession(name: name);

        // Assert
        Assert.Equal(name, session.Name);
    }

    [Fact]
    public void SessionManager_GetSession_ShouldReturnExistingSession()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession();

        // Act
        var retrieved = manager.GetSession(session.Id);

        // Assert
        Assert.Equal(session, retrieved);
    }

    [Fact]
    public void SessionManager_GetSession_WithInvalidId_ShouldReturnNull()
    {
        // Arrange
        var manager = new SessionManager();

        // Act
        var retrieved = manager.GetSession("invalid-id");

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public void SessionManager_DeleteSession_ShouldRemoveSession()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession();
        var sessionId = session.Id;

        // Act
        var deleted = manager.DeleteSession(sessionId);

        // Assert
        Assert.True(deleted);
        Assert.Equal(0, manager.SessionCount);
        Assert.Null(manager.CurrentSession);
        Assert.Equal(SessionStatus.Closed, session.Status);
    }

    [Fact]
    public void SessionManager_DeleteSession_WithInvalidId_ShouldReturnFalse()
    {
        // Arrange
        var manager = new SessionManager();

        // Act
        var deleted = manager.DeleteSession("invalid-id");

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public void SessionManager_SwitchSession_ShouldChangeCurrentSession()
    {
        // Arrange
        var manager = new SessionManager();
        var session1 = manager.CreateSession(name: "Session 1");
        var session2 = manager.CreateSession(name: "Session 2");

        // Act
        var switched = manager.SwitchSession(session1.Id);

        // Assert
        Assert.True(switched);
        Assert.Equal(session1, manager.CurrentSession);
    }

    [Fact]
    public void SessionManager_GetAllSessions_ShouldReturnAllSessions()
    {
        // Arrange
        var manager = new SessionManager();
        manager.CreateSession(name: "Session 1");
        manager.CreateSession(name: "Session 2");
        manager.CreateSession(name: "Session 3");

        // Act
        var sessions = manager.GetAllSessions();

        // Assert
        Assert.Equal(3, sessions.Count);
    }

    [Fact]
    public void SessionManager_GetActiveSessions_ShouldReturnOnlyActiveSessions()
    {
        // Arrange
        var manager = new SessionManager();
        var session1 = manager.CreateSession(name: "Session 1");
        var session2 = manager.CreateSession(name: "Session 2");
        manager.PauseSession(session1.Id);

        // Act
        var activeSessions = manager.GetActiveSessions();

        // Assert
        Assert.Single(activeSessions);
        Assert.Equal(session2.Id, activeSessions[0].Id);
    }

    [Fact]
    public void SessionManager_ClearSessions_ShouldRemoveAllSessions()
    {
        // Arrange
        var manager = new SessionManager();
        manager.CreateSession();
        manager.CreateSession();
        manager.CreateSession();

        // Act
        manager.ClearSessions();

        // Assert
        Assert.Equal(0, manager.SessionCount);
        Assert.Null(manager.CurrentSession);
    }

    [Fact]
    public void SessionManager_SessionExists_ShouldReturnCorrectValue()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession();

        // Act & Assert
        Assert.True(manager.SessionExists(session.Id));
        Assert.False(manager.SessionExists("invalid-id"));
    }

    [Fact]
    public void SessionManager_PauseSession_ShouldChangeStatus()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession();

        // Act
        var paused = manager.PauseSession(session.Id);

        // Assert
        Assert.True(paused);
        Assert.Equal(SessionStatus.Paused, session.Status);
    }

    [Fact]
    public void SessionManager_ResumeSession_ShouldChangeStatus()
    {
        // Arrange
        var manager = new SessionManager();
        var session = manager.CreateSession();
        manager.PauseSession(session.Id);

        // Act
        var resumed = manager.ResumeSession(session.Id);

        // Assert
        Assert.True(resumed);
        Assert.Equal(SessionStatus.Active, session.Status);
    }

    [Fact]
    public void SessionManager_GetSessionsByAgent_ShouldReturnFilteredSessions()
    {
        // Arrange
        var manager = new SessionManager();
        var agentName = "TestAgent";
        manager.CreateSession(name: "Session 1", agentName: agentName);
        manager.CreateSession(name: "Session 2", agentName: "OtherAgent");
        manager.CreateSession(name: "Session 3", agentName: agentName);

        // Act
        var sessions = manager.GetSessionsByAgent(agentName);

        // Assert
        Assert.Equal(2, sessions.Count);
        Assert.All(sessions, s => Assert.Equal(agentName, s.AgentName));
    }

    [Fact]
    public async Task SessionManager_ConcurrentAccess_ShouldBeThreadSafe()
    {
        // Arrange
        var manager = new SessionManager();
        var tasks = new Task[10];

        // Act
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    manager.CreateSession($"Session-{i}-{j}");
                }
            });
        }
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, manager.SessionCount);
    }
}
