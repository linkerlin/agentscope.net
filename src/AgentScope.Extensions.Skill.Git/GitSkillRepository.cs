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

using System.Collections.Concurrent;
using LibGit2Sharp;

namespace AgentScope.Extensions.Skill.Git;

/// <summary>
/// Git 技能仓库。对标 Java GitSkillRepository。
/// 使用 LibGit2Sharp（JGit 的 .NET 等价库）。
/// </summary>
public sealed class GitSkillRepository : ISkillRepository, IDisposable
{
    private readonly string _repoUrl;
    private readonly string _branch;
    private readonly string _localPath;
    private Repository? _repo;
    private ConcurrentDictionary<string, Skill>? _cache;
    private readonly object _sync = new();

    public GitSkillRepository(string repoUrl, string branch = "main", string? localPath = null)
    {
        _repoUrl = repoUrl;
        _branch = branch;
        _localPath = localPath ?? Path.Combine(Path.GetTempPath(), "agentscope_skills", Guid.NewGuid().ToString("N"));
    }

    private void EnsureRepo()
    {
        if (_repo != null) return;
        lock (_sync)
        {
            if (_repo != null) return;

            if (!Directory.Exists(_localPath) || !Repository.IsValid(_localPath))
            {
                Directory.CreateDirectory(_localPath);
                Repository.Clone(_repoUrl, _localPath, new CloneOptions { BranchName = _branch });
            }

            _repo = new Repository(_localPath);
            _cache = new ConcurrentDictionary<string, Skill>(StringComparer.OrdinalIgnoreCase);
            LoadSkills();
        }
    }

    private void LoadSkills()
    {
        if (_repo == null) return;
        var skillFiles = Directory.GetFiles(_localPath, "*.skill.yaml", SearchOption.AllDirectories);
        foreach (var file in skillFiles)
        {
            var name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
            var content = File.ReadAllText(file);
            var skill = new Skill(name, $"从 {_repoUrl} 加载", content, _repoUrl);
            _cache![name] = skill;
        }
    }

    public Task<Skill?> GetSkillAsync(string name, CancellationToken ct = default)
    {
        EnsureRepo();
        return Task.FromResult(_cache?.TryGetValue(name, out var skill) == true ? skill : null);
    }

    public Task<IReadOnlyList<string>> GetAllSkillNamesAsync(CancellationToken ct = default)
    {
        EnsureRepo();
        return Task.FromResult<IReadOnlyList<string>>(_cache?.Keys.ToList() ?? []);
    }

    public Task<bool> SkillExistsAsync(string name, CancellationToken ct = default)
    {
        EnsureRepo();
        return Task.FromResult(_cache?.ContainsKey(name) ?? false);
    }

    public void Sync()
    {
        if (_repo == null) return;
        lock (_sync)
        {
            var remote = _repo.Network.Remotes["origin"];
            if (remote != null)
            {
                var refSpecs = remote.FetchRefSpecs.Select(s => s.Specification);
                Commands.Fetch(_repo, remote.Name, refSpecs, null, null);
                LoadSkills();
            }
        }
    }

    public void Dispose() => _repo?.Dispose();
}
