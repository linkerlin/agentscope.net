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
using System.Threading.Tasks;
using AgentScope.Core.Message;

namespace AgentScope.Core.Agent;

/// <summary>
/// 可观察的 Agent 接口，对应 Java ObservableAgent
/// Agent 通过 observe 接收来自其他 Agent 的消息并做出反应
/// </summary>
public interface IObservableAgent
{
    /// <summary>
    /// 观察并处理消息
    /// </summary>
    Task ObserveAsync(Msg message, RuntimeContext? context = null);

    /// <summary>
    /// 观察并处理多条消息
    /// </summary>
    Task ObserveAsync(IReadOnlyList<Msg> messages, RuntimeContext? context = null);
}
