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

namespace AgentScope.Harness.Skill.Runtime;

/// <summary>将非 workspace 的 skill 资源物化到缓存目录，对应 Java MarketplaceStager</summary>
public sealed class MarketplaceStager
{
    private readonly string _cacheDir;

    public MarketplaceStager(string? cacheDir = null)
    {
        _cacheDir = cacheDir ?? Path.Combine(
            Environment.CurrentDirectory, ".skills-cache");
    }

    public async Task<Dictionary<string, StageResult>> StageAsync(
        IEnumerable<HarnessSkillEntry> entries)
    {
        var results = new Dictionary<string, StageResult>();
        foreach (var entry in entries)
        {
            if (entry.Resources?.Files == null)
            {
                results[entry.SkillId] = StageResult.None;
                continue;
            }

            var skillDir = Path.Combine(_cacheDir, entry.SkillId);
            Directory.CreateDirectory(skillDir);

            foreach (var (path, content) in entry.Resources.Files)
            {
                var fullPath = Path.Combine(skillDir, path);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                await File.WriteAllTextAsync(fullPath, content);
            }

            results[entry.SkillId] = StageResult.Cached(entry.SkillId);
        }

        return results;
    }

    public void InvalidateAll()
    {
        if (Directory.Exists(_cacheDir))
            Directory.Delete(_cacheDir, recursive: true);
    }

    public abstract record StageResult
    {
        public static StageResult None => new NoneResult();
        public static StageResult Cached(string skillId) => new CachedResult(skillId);
    }
    private sealed record NoneResult : StageResult;
    private sealed record CachedResult(string SkillId) : StageResult;
}
