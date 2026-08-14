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

using System.Collections.Generic;
using System.Threading.Tasks;
using AgentScope.Core.Message;
using AgentScope.Core.State;

namespace AgentScope.Core.Memory;

/// <summary>
/// 状态后备内存适配器：把 IMemory 的内存操作委托给 AgentState，
/// 并在每次变更后通过 IAgentStateStore 持久化（支持版本化 CAS）。
/// 对应 Java: io.agentscope.core.memory.StateBackedMemory
/// </summary>
public class StateBackedMemory : IMemory
{
    private readonly AgentState _state;
    private readonly IAgentStateStore _store;
    private readonly string _stateKey;
    private long _currentVersion;
    private readonly SemaphoreSlim _persistGate = new(1, 1);
    private System.Exception? _lastPersistException;

    /// <summary>最近一次后台持久化的异常（为 null 表示成功/尚未运行）。用于不丢失火忘式持久化的错误。</summary>
    public System.Exception? LastPersistException => _lastPersistException;

    /// <summary>
    /// 创建状态后备内存。
    /// </summary>
    /// <param name="store">状态存储（决定是否版本化）。</param>
    /// <param name="initial">初始状态（从 store 加载或新建）。</param>
    /// <param name="stateKey">持久化键名。</param>
    public StateBackedMemory(IAgentStateStore store, AgentState initial, string stateKey = "default")
    {
        _store = store ?? throw new System.ArgumentNullException(nameof(store));
        _state = initial ?? throw new System.ArgumentNullException(nameof(initial));
        _stateKey = stateKey;
    }

    /// <summary>底层状态。</summary>
    public AgentState State => _state;

    /// <inheritdoc />
    public void Add(Msg message)
    {
        _state.Context.Add(message);
        EnqueuePersist();
    }

    /// <inheritdoc />
    public List<Msg> GetAll() => new(_state.Context);

    /// <inheritdoc />
    public List<Msg> GetRecent(int count)
    {
        var start = System.Math.Max(0, _state.Context.Count - count);
        return _state.Context.GetRange(start, _state.Context.Count - start);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _state.Context.Clear();
        EnqueuePersist();
    }

    /// <inheritdoc />
    public int Count() => _state.Context.Count;

    /// <inheritdoc />
    public bool Delete(string messageId)
    {
        var removed = _state.Context.RemoveAll(m => m.Id == messageId);
        if (removed > 0)
        {
            EnqueuePersist();
        }

        return removed > 0;
    }

    /// <summary>
    /// 异步排队一次串行化持久化（不抛、不丢失错误，错误记录到 <see cref="LastPersistException"/>）。
    /// 多次调用按顺序串行执行，避免并发覆盖与乱序。
    /// </summary>
    private void EnqueuePersist() => _ = RunPersistAsync();

    private async Task RunPersistAsync()
    {
        try
        {
            await PersistAsync().ConfigureAwait(false);
            _lastPersistException = null;
        }
        catch (System.Exception ex)
        {
            _lastPersistException = ex;
        }
    }

    /// <summary>显式持久化（串行化执行，带 CAS 重试）。</summary>
    public async Task PersistAsync()
    {
        await _persistGate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_store.SupportsVersioning)
            {
                try
                {
                    _currentVersion = await _store.SaveIfVersionAsync(
                        _state.UserId ?? "", _state.SessionId, _stateKey, _state, _currentVersion).ConfigureAwait(false);
                }
                catch (ConcurrentSessionModificationException)
                {
                    // 版本不匹配：重新加载后再次保存
                    var latest = await _store.GetVersionedAsync(_state.UserId ?? "", _state.SessionId, _stateKey)
                        .ConfigureAwait(false);
                    if (latest != null)
                    {
                        _currentVersion = latest.Version;
                    }

                    _currentVersion = await _store.SaveIfVersionAsync(
                        _state.UserId ?? "", _state.SessionId, _stateKey, _state, _currentVersion).ConfigureAwait(false);
                }
            }
            else
            {
                await _store.SaveAsync(_state.UserId ?? "", _state.SessionId, _stateKey, _state)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            _persistGate.Release();
        }
    }
}
