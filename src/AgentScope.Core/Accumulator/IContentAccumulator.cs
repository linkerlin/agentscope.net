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

using AgentScope.Core.Message;

namespace AgentScope.Core.Accumulator;

/// <summary>
/// Content accumulator: incrementally accumulates ContentBlocks for streaming reasoning/acting stage display.
/// 内容累加器：逐步累积 ContentBlock，用于流式推理/行动阶段的增量展示。
/// </summary>
public interface IContentAccumulator
{
    /// <summary>
    /// Accumulates a single content block.
    /// 累积一个内容块。
    /// </summary>
    /// <param name="block">Content block to accumulate / 要累积的内容块</param>
    void Accumulate(ContentBlock block);

    /// <summary>
    /// Gets the accumulated result, or null if nothing has been accumulated.
    /// 获取累积结果，如果尚未累积任何内容则返回 null。
    /// </summary>
    ContentBlock? GetAccumulated();

    /// <summary>
    /// Resets the accumulator to its initial state.
    /// 将累加器重置为初始状态。
    /// </summary>
    void Reset();
}
