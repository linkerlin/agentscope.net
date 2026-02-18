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
using System.Reactive.Linq;
using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// Interface for all agents
/// </summary>
public interface IAgent
{
    string Name { get; }
    
    IObservable<Msg> Call(Msg message);
    
    Task<Msg> CallAsync(Msg message);
}

/// <summary>
/// Abstract base class for agents
/// </summary>
public abstract class AgentBase : IAgent
{
    public string Name { get; protected set; }

    protected AgentBase(string name)
    {
        Name = name;
    }

    public abstract IObservable<Msg> Call(Msg message);

    public virtual async Task<Msg> CallAsync(Msg message)
    {
        return await Call(message).FirstOrDefaultAsync();
    }
}
