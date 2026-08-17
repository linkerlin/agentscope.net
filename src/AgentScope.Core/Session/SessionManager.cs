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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AgentScope.Core.Session;

/// <summary>
/// Manages the lifecycle of conversation sessions, including creation, switching, and cleanup.
/// 管理对话会话的生命周期，包括创建、切换和清理。
/// 
/// Provides thread-safe access to sessions using ConcurrentDictionary and ReaderWriterLockSlim.
/// 使用 ConcurrentDictionary 和 ReaderWriterLockSlim 提供线程安全的会话访问。
/// 
/// Corresponds to Java: io.agentscope.core.session.SessionManager
/// 对应 Java: io.agentscope.core.session.SessionManager
/// </summary>
public class SessionManager
{
    /// <summary>
    /// Thread-safe dictionary storing all sessions keyed by session ID.
    /// 存储所有会话的线程安全字典，以会话 ID 为键。
    /// </summary>
    private readonly ConcurrentDictionary<string, Session> _sessions;

    /// <summary>
    /// Reader-writer lock for thread-safe access to the current session.
    /// 用于线程安全访问当前会话的读写锁。
    /// </summary>
    private readonly ReaderWriterLockSlim _lock;

    /// <summary>
    /// The currently active session.
    /// 当前活跃的会话。
    /// </summary>
    private Session? _currentSession;

    /// <summary>
    /// Initializes a new instance of SessionManager.
    /// 初始化 SessionManager 的新实例。
    /// </summary>
    public SessionManager()
    {
        _sessions = new ConcurrentDictionary<string, Session>();
        _lock = new ReaderWriterLockSlim();
    }

    /// <summary>
    /// Gets or sets the current active session with thread-safe access.
    /// 获取或设置当前活跃的会话，具有线程安全访问。
    /// </summary>
    public Session? CurrentSession
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _currentSession;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        private set
        {
            _lock.EnterWriteLock();
            try
            {
                _currentSession = value;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Creates a new session and sets it as the current session.
    /// 创建新会话并将其设置为当前会话。
    /// </summary>
    /// <param name="name">Optional session name. 可选的会话名称。</param>
    /// <param name="agentName">Optional associated agent name. 可选的相关联 Agent 名称。</param>
    /// <returns>The newly created Session instance. 新创建的 Session 实例。</returns>
    public Session CreateSession(string? name = null, string? agentName = null)
    {
        var session = new Session(name: name)
        {
            AgentName = agentName
        };

        _sessions.TryAdd(session.Id, session);
        CurrentSession = session;

        return session;
    }

    /// <summary>
    /// Retrieves a session by its ID.
    /// 通过 ID 检索会话。
    /// </summary>
    /// <param name="sessionId">The session ID to look up. 要查找的会话 ID。</param>
    /// <returns>The session if found; otherwise null. 如果找到则返回会话；否则返回 null。</returns>
    public Session? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    /// <summary>
    /// Deletes a session by its ID, marking it as closed.
    /// 通过 ID 删除会话，将其标记为已关闭。
    /// </summary>
    /// <param name="sessionId">The session ID to delete. 要删除的会话 ID。</param>
    /// <returns>True if the session was found and deleted; otherwise false.
    /// 如果找到并删除了会话则返回 true；否则返回 false。</returns>
    public bool DeleteSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var session))
        {
            session.Status = SessionStatus.Closed;
            
            if (CurrentSession?.Id == sessionId)
            {
                CurrentSession = null;
            }
            
            return true;
        }
        return false;
    }

    /// <summary>
    /// Switches the current session to the specified session ID.
    /// 将当前会话切换到指定的会话 ID。
    /// </summary>
    /// <param name="sessionId">The session ID to switch to. 要切换到的会话 ID。</param>
    /// <returns>True if the session was found and switched; otherwise false.
    /// 如果找到并切换了会话则返回 true；否则返回 false。</returns>
    public bool SwitchSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            CurrentSession = session;
            session.Touch();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all sessions regardless of status.
    /// 获取所有会话，无论状态如何。
    /// </summary>
    /// <returns>A read-only list of all sessions. 所有会话的只读列表。</returns>
    public IReadOnlyList<Session> GetAllSessions()
    {
        return _sessions.Values.ToList();
    }

    /// <summary>
    /// Gets only the active sessions.
    /// 仅获取活跃的会话。
    /// </summary>
    /// <returns>A read-only list of active sessions. 活跃会话的只读列表。</returns>
    public IReadOnlyList<Session> GetActiveSessions()
    {
        return _sessions.Values
            .Where(s => s.Status == SessionStatus.Active)
            .ToList();
    }

    /// <summary>
    /// Clears all sessions, marking each as closed.
    /// 清空所有会话，将每个标记为已关闭。
    /// </summary>
    public void ClearSessions()
    {
        foreach (var session in _sessions.Values)
        {
            session.Status = SessionStatus.Closed;
        }
        
        _sessions.Clear();
        CurrentSession = null;
    }

    /// <summary>
    /// Gets the total number of sessions.
    /// 获取会话总数。
    /// </summary>
    public int SessionCount => _sessions.Count;

    /// <summary>
    /// Checks if a session with the specified ID exists.
    /// 检查具有指定 ID 的会话是否存在。
    /// </summary>
    /// <param name="sessionId">The session ID to check. 要检查的会话 ID。</param>
    /// <returns>True if the session exists; otherwise false. 如果会话存在则返回 true；否则返回 false。</returns>
    public bool SessionExists(string sessionId)
    {
        return _sessions.ContainsKey(sessionId);
    }

    /// <summary>
    /// Pauses a session, changing its status to Paused.
    /// 暂停会话，将其状态更改为 Paused。
    /// </summary>
    /// <param name="sessionId">The session ID to pause. 要暂停的会话 ID。</param>
    /// <returns>True if the session was found and paused; otherwise false.
    /// 如果找到并暂停了会话则返回 true；否则返回 false。</returns>
    public bool PauseSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Status = SessionStatus.Paused;
            session.Touch();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Resumes a paused session, changing its status back to Active.
    /// 恢复暂停的会话，将其状态更改回 Active。
    /// </summary>
    /// <param name="sessionId">The session ID to resume. 要恢复的会话 ID。</param>
    /// <returns>True if the session was found and resumed; otherwise false.
    /// 如果找到并恢复了会话则返回 true；否则返回 false。</returns>
    public bool ResumeSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            session.Status = SessionStatus.Active;
            session.Touch();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Gets all sessions associated with a specific agent.
    /// 获取与特定 Agent 关联的所有会话。
    /// </summary>
    /// <param name="agentName">The agent name to filter by. 要筛选的 Agent 名称。</param>
    /// <returns>A read-only list of sessions for the specified agent. 指定 Agent 的会话只读列表。</returns>
    public IReadOnlyList<Session> GetSessionsByAgent(string agentName)
    {
        return _sessions.Values
            .Where(s => s.AgentName == agentName)
            .ToList();
    }
}
