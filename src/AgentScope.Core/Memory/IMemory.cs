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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Memory;

/// <summary>
/// Agent memory interface for storing and retrieving conversation messages.
/// Agent 记忆接口 - 用于存储和检索对话消息。
/// Corresponds to Java: io.agentscope.memory.Memory
/// </summary>
public interface IMemory
{
    /// <summary>
    /// Adds a message to memory.
    /// 向记忆中添加一条消息。
    /// </summary>
    /// <param name="message">The message to add. / 要添加的消息。</param>
    void Add(Msg message);
    
    /// <summary>
    /// Gets all messages from memory.
    /// 获取记忆中的所有消息。
    /// </summary>
    /// <returns>List of all messages. / 所有消息的列表。</returns>
    List<Msg> GetAll();
    
    /// <summary>
    /// Gets the most recent messages from memory.
    /// 获取记忆中最新的消息。
    /// </summary>
    /// <param name="count">Number of recent messages to retrieve. / 要检索的最新消息数量。</param>
    /// <returns>List of recent messages. / 最新消息的列表。</returns>
    List<Msg> GetRecent(int count);
    
    /// <summary>
    /// Clears all messages from memory.
    /// 清除记忆中的所有消息。
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Gets the total count of messages in memory.
    /// 获取记忆中的消息总数。
    /// </summary>
    /// <returns>The message count. / 消息数量。</returns>
    int Count();

    /// <summary>
    /// Deletes a message by its ID.
    /// 按消息 ID 删除消息。
    /// </summary>
    /// <param name="messageId">The ID of the message to delete. / 要删除的消息的 ID。</param>
    /// <returns>True if the message was found and deleted. / 如果找到并删除了消息则返回 true。</returns>
    bool Delete(string messageId);
}

/// <summary>
/// In-memory implementation of IMemory.
/// IMemory 的内存实现 - 使用线程安全的列表存储消息。
/// Corresponds to Java: io.agentscope.memory.MemoryBase
/// </summary>
public class MemoryBase : IMemory
{
    /// <summary>
    /// Internal list storing all messages.
    /// 存储所有消息的内部列表。
    /// </summary>
    private readonly List<Msg> _messages = new();

    /// <summary>
    /// Lock object for thread safety.
    /// 用于线程安全的锁对象。
    /// </summary>
    private readonly object _lock = new();

    /// <inheritdoc />
    public void Add(Msg message)
    {
        lock (_lock)
        {
            _messages.Add(message);
        }
    }

    /// <inheritdoc />
    public List<Msg> GetAll()
    {
        lock (_lock)
        {
            return new List<Msg>(_messages);
        }
    }

    /// <inheritdoc />
    public List<Msg> GetRecent(int count)
    {
        lock (_lock)
        {
            return _messages.Skip(System.Math.Max(0, _messages.Count - count)).ToList();
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
    }

    /// <inheritdoc />
    public int Count()
    {
        lock (_lock)
        {
            return _messages.Count;
        }
    }

    /// <inheritdoc />
    public bool Delete(string messageId)
    {
        lock (_lock)
        {
            var removed = _messages.RemoveAll(m => m.Id == messageId);
            return removed > 0;
        }
    }
}

/// <summary>
/// Persistent memory interface with search capability.
/// 支持搜索功能的持久化记忆接口。
/// Corresponds to Java: io.agentscope.memory.PersistentMemory
/// </summary>
public interface IPersistentMemory : IMemory
{
    /// <summary>
    /// Searches messages by query text.
    /// 按查询文本搜索消息。
    /// </summary>
    /// <param name="query">The search query. / 搜索查询。</param>
    /// <param name="limit">Maximum number of results. / 最大结果数。</param>
    /// <returns>List of matching messages. / 匹配的消息列表。</returns>
    Task<List<Msg>> SearchAsync(string query, int limit = 10);
    
    /// <summary>
    /// Saves memory state to persistent storage.
    /// 将记忆状态保存到持久化存储。
    /// </summary>
    Task SaveAsync();
    
    /// <summary>
    /// Loads memory state from persistent storage.
    /// 从持久化存储加载记忆状态。
    /// </summary>
    Task LoadAsync();
}
