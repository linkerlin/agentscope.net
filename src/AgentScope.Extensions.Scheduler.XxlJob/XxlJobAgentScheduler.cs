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
