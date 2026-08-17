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

using System.Runtime.CompilerServices;

namespace AgentScope.Extensions;

/// <summary>
/// Helper methods to convert synchronous IEnumerable to IAsyncEnumerable.
/// 将同步 IEnumerable 转换为 IAsyncEnumerable 的辅助方法。
/// </summary>
public static class AsyncEnumerableHelper
{
    /// <summary>
    /// Converts an IEnumerable&lt;T&gt; to an IAsyncEnumerable&lt;T&gt;.
    /// 将 IEnumerable&lt;T&gt; 转换为 IAsyncEnumerable&lt;T&gt;。
    /// </summary>
    /// <typeparam name="T">The type of elements in the source. 源中元素的类型。</typeparam>
    /// <param name="source">The synchronous enumerable source. 同步可枚举源。</param>
    /// <returns>An async-enumerable sequence. 一个异步可枚举序列。</returns>
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
            yield return item;
    }
}
