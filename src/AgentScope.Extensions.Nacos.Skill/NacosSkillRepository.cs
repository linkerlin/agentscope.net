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

using System.Text.Json;

namespace AgentScope.Extensions.Nacos.Skill;

public sealed class NacosSkillRepository
{
    private readonly string _serverAddr;
    private readonly string _namespaceId;
    private readonly string _group;
    private readonly HttpClient _http;

    public NacosSkillRepository(string serverAddr, string? namespaceId = null, string? group = null, HttpClient? http = null)
    {
        _serverAddr = serverAddr.TrimEnd('/');
        _namespaceId = namespaceId ?? "public";
        _group = group ?? "DEFAULT_GROUP";
        _http = http ?? new HttpClient();
    }

    public async Task<string?> GetSkillContentAsync(string skillId, CancellationToken ct = default)
    {
        var url = $"{_serverAddr}/nacos/v1/cs/configs?dataId=skill.{skillId}&group={_group}&tenant={_namespaceId}";
        var resp = await _http.GetAsync(url, ct);
        return resp.IsSuccessStatusCode ? await resp.Content.ReadAsStringAsync(ct) : null;
    }

    public async Task PublishSkillAsync(string skillId, string content, CancellationToken ct = default)
    {
        var url = $"{_serverAddr}/nacos/v1/cs/configs";
        var form = new Dictionary<string, string>
        {
            ["dataId"] = $"skill.{skillId}",
            ["group"] = _group,
            ["tenant"] = _namespaceId,
            ["content"] = content
        };
        var resp = await _http.PostAsync(url, new FormUrlEncodedContent(form), ct);
        resp.EnsureSuccessStatusCode();
    }
}
