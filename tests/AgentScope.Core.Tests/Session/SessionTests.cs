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

/// <summary>
/// Unit tests for the <see cref="Core.Session.Session"/> entity, verifying construction, property accessors,
/// context/metadata operations, and status transitions.
/// 对 <see cref="Core.Session.Session"/> 实体的单元测试，验证构造、属性访问器、上下文/元数据操作以及状态转换。
/// </summary>
public class SessionTests
{
    /// <summary>
    /// Tests that the default constructor initializes all properties with sensible defaults.
    /// 测试默认构造函数是否使用合理的默认值初始化所有属性。
    /// </summary>
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

    /// <summary>
    /// Tests that a session created with a custom name uses the provided name.
    /// 测试使用自定义名称创建的会话是否使用了提供的名称。
    /// </summary>
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

    /// <summary>
    /// Tests that a session created with a custom ID uses the provided ID.
    /// 测试使用自定义 ID 创建的会话是否使用了提供的 ID。
    /// </summary>
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

    /// <summary>
    /// Tests that calling <see cref="Core.Session.Session.Touch"/> advances the UpdatedAt timestamp.
    /// 测试调用 <see cref="Core.Session.Session.Touch"/> 是否更新了 UpdatedAt 时间戳。
    /// </summary>
    [Fact]
    public void Session_Touch_ShouldUpdateTimestamp()
    {
        // Arrange
        var session = new Core.Session.Session();
        var originalTime = session.UpdatedAt;
        
        // 等待一点时间以确保时间差异
        System.Threading.Thread.Sleep(10);

        // Act
        session.Touch();

        // Assert
        Assert.True(session.UpdatedAt > originalTime);
    }

    /// <summary>
    /// Tests that context values can be stored and retrieved correctly.
    /// 测试上下文值能否正确存储和读取。
    /// </summary>
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

    /// <summary>
    /// Tests that retrieving a context value with the wrong type returns default(T).
    /// 测试使用错误类型读取上下文值时是否返回 default(T)。
    /// </summary>
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

    /// <summary>
    /// Tests that metadata values can be stored and retrieved correctly.
    /// 测试元数据值能否正确存储和读取。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="Core.Session.Session.SetContext"/> automatically touches the session (updates UpdatedAt).
    /// 测试 <see cref="Core.Session.Session.SetContext"/> 是否自动更新会话的 UpdatedAt 时间戳。
    /// </summary>
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

    /// <summary>
    /// Tests that the AgentName property can be set and read back.
    /// 测试 AgentName 属性能否正确设置和读取。
    /// </summary>
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

    /// <summary>
    /// Tests that the Status property can be changed to a different <see cref="SessionStatus"/>.
    /// 测试 Status 属性能否切换到不同的 <see cref="SessionStatus"/> 值。
    /// </summary>
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

/// <summary>
/// Unit tests for the <see cref="SessionManager"/> class, verifying full lifecycle management
/// including creation, retrieval, deletion, switching, filtering, and thread safety.
/// 对 <see cref="SessionManager"/> 类的单元测试，验证包括创建、检索、删除、切换、筛选和线程安全的完整生命周期管理。
/// </summary>
public class SessionManagerTests
{
    /// <summary>
    /// Tests that <see cref="SessionManager.CreateSession()"/> returns a new session and sets it as current.
    /// 测试 <see cref="SessionManager.CreateSession()"/> 返回新会话并将其设为当前会话。
    /// </summary>
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

    /// <summary>
    /// Tests that a session created with a custom name retains that name.
    /// 测试使用自定义名称创建的会话是否保留了该名称。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.GetSession"/> retrieves an existing session by ID.
    /// 测试 <see cref="SessionManager.GetSession"/> 能否通过 ID 检索到已存在的会话。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.GetSession"/> returns null for an unknown ID.
    /// 测试 <see cref="SessionManager.GetSession"/> 对未知 ID 是否返回 null。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.DeleteSession"/> removes the session and updates state.
    /// 测试 <see cref="SessionManager.DeleteSession"/> 是否移除会话并更新状态。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.DeleteSession"/> returns false for an unknown ID.
    /// 测试 <see cref="SessionManager.DeleteSession"/> 对未知 ID 是否返回 false。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.SwitchSession"/> changes the current session.
    /// 测试 <see cref="SessionManager.SwitchSession"/> 是否切换当前会话。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.GetAllSessions"/> returns all created sessions.
    /// 测试 <see cref="SessionManager.GetAllSessions"/> 是否返回所有已创建的会话。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.GetActiveSessions"/> excludes paused sessions.
    /// 测试 <see cref="SessionManager.GetActiveSessions"/> 是否排除了暂停的会话。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.ClearSessions"/> removes all sessions and resets current.
    /// 测试 <see cref="SessionManager.ClearSessions"/> 是否移除所有会话并重置当前会话。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.SessionExists"/> returns true only for known IDs.
    /// 测试 <see cref="SessionManager.SessionExists"/> 是否仅对已知 ID 返回 true。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.PauseSession"/> changes the session status to Paused.
    /// 测试 <see cref="SessionManager.PauseSession"/> 是否将会话状态改为 Paused。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.ResumeSession"/> changes the session status back to Active.
    /// 测试 <see cref="SessionManager.ResumeSession"/> 是否将会话状态恢复为 Active。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager.GetSessionsByAgent"/> correctly filters sessions by agent name.
    /// 测试 <see cref="SessionManager.GetSessionsByAgent"/> 是否按 agent 名称正确筛选会话。
    /// </summary>
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

    /// <summary>
    /// Tests that <see cref="SessionManager"/> handles concurrent session creation correctly (thread safety).
    /// 测试 <see cref="SessionManager"/> 能否正确处理并发的会话创建（线程安全性）。
    /// </summary>
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
