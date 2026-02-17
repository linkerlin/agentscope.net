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
/// Interface for agent memory
/// </summary>
public interface IMemory
{
    void Add(Msg message);
    
    List<Msg> GetAll();
    
    List<Msg> GetRecent(int count);
    
    void Clear();
    
    int Count();
}

/// <summary>
/// In-memory implementation of IMemory
/// </summary>
public class MemoryBase : IMemory
{
    private readonly List<Msg> _messages = new();
    private readonly object _lock = new();

    public void Add(Msg message)
    {
        lock (_lock)
        {
            _messages.Add(message);
        }
    }

    public List<Msg> GetAll()
    {
        lock (_lock)
        {
            return new List<Msg>(_messages);
        }
    }

    public List<Msg> GetRecent(int count)
    {
        lock (_lock)
        {
            return _messages.Skip(System.Math.Max(0, _messages.Count - count)).ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _messages.Clear();
        }
    }

    public int Count()
    {
        lock (_lock)
        {
            return _messages.Count;
        }
    }
}

/// <summary>
/// Interface for persistent memory with search capabilities
/// </summary>
public interface IPersistentMemory : IMemory
{
    Task<List<Msg>> SearchAsync(string query, int limit = 10);
    
    Task SaveAsync();
    
    Task LoadAsync();
}
