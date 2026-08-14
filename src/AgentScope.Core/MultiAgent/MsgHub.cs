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
using AgentScope.Core.Message;

namespace AgentScope.Core.MultiAgent;

public sealed class MsgHub
{
    private readonly HashSet<(string Name, IObserver<Msg> Obs)> _subs = new();

    public IDisposable Subscribe(string agentName, IObserver<Msg> observer)
    {
        var entry = (agentName, observer);
        lock (_subs)
        {
            _subs.Add(entry);
        }
        return new Subscription(this, agentName, observer);
    }

    public void RemoveSub((string, IObserver<Msg>) key)
    {
        lock (_subs)
        {
            _subs.Remove(key);
        }
    }

    public void Publish(string from, Msg msg)
    {
        List<IObserver<Msg>> targets;
        lock (_subs)
        {
            targets = _subs
                .Where(s => s.Name != from)
                .Select(s => s.Obs)
                .ToList();
        }

        foreach (var obs in targets)
        {
            obs.OnNext(msg);
        }
    }

    public void SendTo(string target, Msg msg)
    {
        List<IObserver<Msg>> targets;
        lock (_subs)
        {
            targets = _subs
                .Where(s => s.Name == target)
                .Select(s => s.Obs)
                .ToList();
        }

        foreach (var obs in targets)
        {
            obs.OnNext(msg);
        }
    }

    private sealed class Subscription : IDisposable
    {
        private readonly MsgHub _hub;
        private readonly string _name;
        private readonly IObserver<Msg> _observer;

        public Subscription(MsgHub hub, string name, IObserver<Msg> observer)
        {
            _hub = hub;
            _name = name;
            _observer = observer;
        }

        public void Dispose()
        {
            _hub.RemoveSub((_name, _observer));
        }
    }
}
