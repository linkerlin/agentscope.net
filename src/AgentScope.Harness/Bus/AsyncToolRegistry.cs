using System.Collections.Concurrent;
using AgentScope.Core.Tool;

namespace AgentScope.Harness.Bus;

/// <summary>异步工具注册中心，对应 Java AsyncToolRegistry</summary>
public interface IAsyncToolRegistry
{
    void Register(string taskId, ITool tool);
    ITool? Resolve(string taskId);
    bool Unregister(string taskId);
}

/// <summary>默认异步工具注册中心实现</summary>
public sealed class AsyncToolRegistry : IAsyncToolRegistry
{
    private readonly ConcurrentDictionary<string, ITool> _tools = new();

    public void Register(string taskId, ITool tool) => _tools[taskId] = tool;
    public ITool? Resolve(string taskId) => _tools.TryGetValue(taskId, out var t) ? t : null;
    public bool Unregister(string taskId) => _tools.TryRemove(taskId, out _);
}
