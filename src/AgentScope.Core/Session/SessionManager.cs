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
/// Session 管理器，负责创建、管理和持久化 Session
/// Session manager for creating, managing and persisting sessions
/// </summary>
public class SessionManager
{
    private readonly ConcurrentDictionary<string, Session> _sessions;
    private readonly ReaderWriterLockSlim _lock;
    private Session? _currentSession;

    public SessionManager()
    {
        _sessions = new ConcurrentDictionary<string, Session>();
        _lock = new ReaderWriterLockSlim();
    }

    /// <summary>
    /// 当前活跃的 Session
    /// Current active session
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
    /// 创建新的 Session
    /// Create a new session
    /// </summary>
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
    /// 获取 Session
    /// Get session by ID
    /// </summary>
    public Session? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    /// <summary>
    /// 删除 Session
    /// Delete session
    /// </summary>
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
    /// 切换到指定的 Session
    /// Switch to specified session
    /// </summary>
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
    /// 获取所有 Session
    /// Get all sessions
    /// </summary>
    public IReadOnlyList<Session> GetAllSessions()
    {
        return _sessions.Values.ToList();
    }

    /// <summary>
    /// 获取活跃的 Sessions
    /// Get active sessions
    /// </summary>
    public IReadOnlyList<Session> GetActiveSessions()
    {
        return _sessions.Values
            .Where(s => s.Status == SessionStatus.Active)
            .ToList();
    }

    /// <summary>
    /// 清空所有 Session
    /// Clear all sessions
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
    /// 获取 Session 数量
    /// Get session count
    /// </summary>
    public int SessionCount => _sessions.Count;

    /// <summary>
    /// 检查 Session 是否存在
    /// Check if session exists
    /// </summary>
    public bool SessionExists(string sessionId)
    {
        return _sessions.ContainsKey(sessionId);
    }

    /// <summary>
    /// 暂停 Session
    /// Pause session
    /// </summary>
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
    /// 恢复 Session
    /// Resume session
    /// </summary>
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
    /// 获取 Agent 相关的所有 Sessions
    /// Get all sessions for an agent
    /// </summary>
    public IReadOnlyList<Session> GetSessionsByAgent(string agentName)
    {
        return _sessions.Values
            .Where(s => s.AgentName == agentName)
            .ToList();
    }
}
