using System.Runtime.CompilerServices;

namespace AgentScope.Extensions;

/// <summary>
/// 将同步 IEnumerable 转换为 IAsyncEnumerable 的辅助方法。
/// </summary>
public static class AsyncEnumerableHelper
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
            yield return item;
    }
}
