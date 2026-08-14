// Copyright 2024-2026 the original author or authors.
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

namespace AgentScope.Core.Shutdown;

/// <summary>
/// 中断/关闭后，对模型已产出的部分推理结果的处理策略。
/// 对应 Java: io.agentscope.core.shutdown.PartialReasoningPolicy
/// </summary>
public enum PartialReasoningPolicy
{
    /// <summary>丢弃部分推理结果（默认）。</summary>
    Discard,

    /// <summary>保留部分推理结果，作为最终回复返回。</summary>
    Keep,

    /// <summary>保留并以“被中断”标记返回，交由上层处理。</summary>
    KeepAsInterrupted
}
