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

namespace AgentScope.Core.State;

/// <summary>
/// 列表哈希工具：对消息/内容列表生成稳定哈希，用于版本化状态比较与脏检查。
/// 对应 Java: io.agentscope.core.state.ListHashUtil
/// </summary>
public static class ListHashUtil
{
    /// <summary>
    /// 计算字符串列表的稳定哈希（顺序敏感）。
    /// </summary>
    public static long Hash(IEnumerable<string?> items)
    {
        const long prime = 1125899906842597L; // 大素数
        long hash = 17L;
        if (items == null)
        {
            return hash;
        }

        foreach (var item in items)
        {
            hash = hash * prime + (item?.GetHashCode() ?? 0);
        }

        return hash;
    }

    /// <summary>
    /// 计算对象列表（按字符串化）的稳定哈希。
    /// </summary>
    public static long HashObjects(IEnumerable<object?> items)
    {
        return Hash(items?.Select(i => i?.ToString()));
    }
}
