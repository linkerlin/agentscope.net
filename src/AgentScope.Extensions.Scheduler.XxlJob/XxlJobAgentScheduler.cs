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

using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Scheduler.XxlJob;

public sealed class XxlJobAgentScheduler : IAgentScheduler
{
    private readonly HttpClient _http;
    private readonly string _adminUrl;
    private readonly string _appName;
    private readonly Dictionary<string, ScheduleAgentTask> _tasks = new();

    public XxlJobAgentScheduler(HttpClient http, string adminUrl, string appName = "agentscope")
    {
        _http = http;
        _adminUrl = adminUrl.TrimEnd('/');
        _appName = appName;
    }

    public async Task<string> ScheduleAsync(ScheduleAgentTask task, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_adminUrl}/jobinfo/addJob", new
        {
            job_group = _appName,
            job_desc = task.AgentName,
            job_cron = task.CronExpression,
            executor_handler = "agentTaskJobHandler",
            executor_param = System.Text.Json.JsonSerializer.Serialize(task.InputParams ?? new Dictionary<string, object>()),
            trigger_status = 1
        }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        var id = json.GetProperty("content").GetProperty("id").GetInt64().ToString();
        _tasks[id] = task;
        return id;
    }

    public async Task CancelAsync(string taskId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_adminUrl}/jobinfo/remove", new { id = taskId }, ct);
        resp.EnsureSuccessStatusCode();
        _tasks.Remove(taskId);
    }

    public async Task<IReadOnlyList<ScheduleAgentTask>> ListTasksAsync(CancellationToken ct = default)
    {
        // 对标 Java getAllScheduleAgentTasks：优先返回本地注册表；再尝试从 XXL-Job admin 分页查询
        if (_tasks.Count > 0)
            return _tasks.Values.ToList();

        var resp = await _http.PostAsync($"{_adminUrl}/jobinfo/pageList", null, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);

        var tasks = new List<ScheduleAgentTask>();
        if (json.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.Array)
        {
            foreach (var job in content.EnumerateArray())
            {
                var id = job.TryGetProperty("id", out var i) ? i.GetInt64().ToString() : "";
                var name = job.TryGetProperty("jobDesc", out var d) ? d.GetString() ?? "" : "";
                var cron = job.TryGetProperty("scheduleConf", out var c) ? c.GetString() ?? "" : "";
                tasks.Add(new ScheduleAgentTask(id, name, cron));
            }
        }
        return tasks;
    }
}
