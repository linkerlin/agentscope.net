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

using AgentScope.Core.Tool;
using AgentScope.Harness.Filesystem;

namespace AgentScope.Harness.Skill;

/// <summary>
/// 技能运行期共享资源集合（工作区文件系统、工具箱、技能仓库等）。
/// 对应 Java: io.agentscope.harness.agent.skill.SkillResources
/// </summary>
public sealed class SkillResources
{
    /// <summary>工作区文件系统（可能为沙箱/本地）。</summary>
    public IFilesystem? Filesystem { get; set; }

    /// <summary>共享工具箱。</summary>
    public Toolkit? Toolkit { get; set; }

    /// <summary>技能根目录。</summary>
    public string? SkillsRoot { get; set; }

    /// <summary>临时目录。</summary>
    public string? TempDir { get; set; }

    public static SkillResources Empty => new();
}

/// <summary>
/// 支持延迟初始化资源的能力接口（资源在首次使用时才创建）。
/// 对应 Java: io.agentscope.harness.agent.skill.LazyResourceCapable
/// </summary>
public interface ILazyResourceCapable<T> where T : class
{
    /// <summary>获取（必要时初始化）资源。</summary>
    Task<T> GetOrCreateAsync(CancellationToken cancellationToken = default);

    /// <summary>资源是否已初始化。</summary>
    bool IsInitialized { get; }
}

/// <summary>
/// 延迟资源基类：线程安全的单次初始化。
/// </summary>
public abstract class LazyResourceCapable<T> : ILazyResourceCapable<T> where T : class
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private T? _value;

    public bool IsInitialized => _value != null;

    public async Task<T> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        if (_value != null) return _value;
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_value != null) return _value;
            _value = await CreateAsync(cancellationToken).ConfigureAwait(false);
            return _value;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>子类实现：创建资源。</summary>
    protected abstract Task<T> CreateAsync(CancellationToken cancellationToken);
}
