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

using AgentScope.Core.Agent;

namespace AgentScope.Core.A2A.Server.Executor.Runner;

/// <summary>
/// 默认 AgentRunner 实现。对标 Java ReActAgentWithBuilderRunner。
/// 每次调用使用 Builder 创建新 Agent 实例；taskId 缓存与中断语义由基类承担。
/// </summary>
public sealed class ReActAgentWithBuilderRunner(Func<IAgent> agentFactory, string name, string description)
    : BaseReActAgentRunner
{
    public override string AgentName => name;
    public override string AgentDescription => description;

    protected override IAgent BuildAgent() => agentFactory();
}
