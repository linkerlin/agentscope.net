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

/// <summary>
/// A simple in-memory message hub for publish/subscribe communication between agents.
/// Agents can subscribe to receive messages, and messages can be published to all subscribers
/// (excluding the sender) or sent to a specific target agent.
/// Corresponds to Java: io.agentscope.core.multiagent.MsgHub
/// 一个简单的内存消息中心，用于 Agent 之间的发布/订阅通信。
/// Agent 可以订阅以接收消息，消息可以发布给所有订阅者（排除发送者）或发送给特定目标 Agent。
/// 对应 Java: io.agentscope.core.multiagent.MsgHub
/// </summary>
public sealed class MsgHub
{
    /// <summary>
    /// Thread-safe set of subscriptions, each containing an agent name and an observer.
    /// 线程安全的订阅集合，每个包含 Agent 名称和观察者。
    /// </summary>
    private readonly HashSet<(string Name, IObserver<Msg> Obs)> _subs = new();

    /// <summary>
    /// Subscribes an agent (by name) to receive messages via the given observer.
    /// Returns an IDisposable that can be used to unsubscribe.
    /// 订阅指定名称的 Agent，通过给定的观察者接收消息。
    /// 返回可用于取消订阅的 IDisposable。
    /// </summary>
    /// <param name="agentName">Name of the subscribing agent / 订阅 Agent 的名称</param>
    /// <param name="observer">Observer to receive messages / 接收消息的观察者</param>
    /// <returns>A disposable subscription handle / 可释放的订阅句柄</returns>
    public IDisposable Subscribe(string agentName, IObserver<Msg> observer)
    {
        var entry = (agentName, observer);
        lock (_subs)
        {
            _subs.Add(entry);
        }
        return new Subscription(this, agentName, observer);
    }

    /// <summary>
    /// Removes a subscription by its key (agent name + observer).
    /// 通过键（Agent 名称 + 观察者）移除订阅。
    /// </summary>
    /// <param name="key">The subscription key to remove / 要移除的订阅键</param>
    public void RemoveSub((string, IObserver<Msg>) key)
    {
        lock (_subs)
        {
            _subs.Remove(key);
        }
    }

    /// <summary>
    /// Publishes a message to all subscribers except the sender.
    /// 将消息发布给除发送者之外的所有订阅者。
    /// </summary>
    /// <param name="from">Name of the sending agent / 发送 Agent 的名称</param>
    /// <param name="msg">Message to publish / 要发布的消息</param>
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

    /// <summary>
    /// Sends a message to a specific target agent by name.
    /// 将消息发送给指定名称的目标 Agent。
    /// </summary>
    /// <param name="target">Name of the target agent / 目标 Agent 的名称</param>
    /// <param name="msg">Message to send / 要发送的消息</param>
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

    /// <summary>
    /// Represents a single subscription that can be disposed to unsubscribe.
    /// 表示一个可释放以取消订阅的单个订阅。
    /// </summary>
    private sealed class Subscription : IDisposable
    {
        private readonly MsgHub _hub;
        private readonly string _name;
        private readonly IObserver<Msg> _observer;

        /// <summary>
        /// Initializes a new subscription.
        /// 初始化一个新的订阅。
        /// </summary>
        /// <param name="hub">The parent MsgHub / 父级 MsgHub</param>
        /// <param name="name">Agent name / Agent 名称</param>
        /// <param name="observer">Observer / 观察者</param>
        public Subscription(MsgHub hub, string name, IObserver<Msg> observer)
        {
            _hub = hub;
            _name = name;
            _observer = observer;
        }

        /// <summary>
        /// Unsubscribes by removing this subscription from the hub.
        /// 通过从消息中心移除本订阅来取消订阅。
        /// </summary>
        public void Dispose()
        {
            _hub.RemoveSub((_name, _observer));
        }
    }
}
