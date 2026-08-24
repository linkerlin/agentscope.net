# Scheduler

`AgentScope.Extensions.Scheduler` runs Agents on a periodic schedule. The module abstracts a unified `IAgentScheduler` interface and ships two implementations:

| Sub-module | Implementation | Deployment |
| --- | --- | --- |
| `AgentScope.Extensions.Scheduler.Quartz` | [Quartz.NET](https://www.quartz-scheduler.net/) | Standalone or clustered |
| `AgentScope.Extensions.Scheduler.XxlJob` | [XXL-Job](https://www.xuxueli.com/xxl-job/) | Distributed, requires Admin Server |

## Common Interface

### IAgentScheduler

| Method | Description |
| --- | --- |
| `Task<string> ScheduleAsync(ScheduleAgentTask task, CancellationToken ct)` | Register a scheduled Agent task; returns the task ID |
| `Task CancelAsync(string taskId, CancellationToken ct)` | Cancel a registered task |
| `Task<IReadOnlyList<ScheduleAgentTask>> ListTasksAsync(CancellationToken ct)` | List all registered tasks |

### ScheduleAgentTask

```csharp
public sealed record ScheduleAgentTask(
    string TaskId,
    string AgentName,
    string CronExpression,
    IDictionary<string, object>? InputParams = null);
```

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Scheduler.Quartz" Version="2.0.1" />
<!-- or -->
<PackageReference Include="AgentScope.Extensions.Scheduler.XxlJob" Version="2.0.1" />
```

## Quartz

```csharp
using AgentScope.Extensions.Scheduler;
using AgentScope.Extensions.Scheduler.Quartz;

var scheduler = new QuartzAgentScheduler(
    agentResolver: name => agentFactory(name));

var taskId = await scheduler.ScheduleAsync(new ScheduleAgentTask(
    TaskId: "daily-report",
    AgentName: "DailyReportAgent",
    CronExpression: "0 0 8 * * ?",
    InputParams: new Dictionary<string, object> { ["text"] = "Generate daily report" }
));

// Runtime control
await scheduler.CancelAsync("daily-report");
var tasks = await scheduler.ListTasksAsync();
```

`QuartzAgentScheduler(Func<string, IAgent>? agentResolver)` — when a resolver is provided, each trigger calls the resolver to obtain the Agent instance and executes it.

## XXL-Job

```csharp
using AgentScope.Extensions.Scheduler.XxlJob;

var scheduler = new XxlJobAgentScheduler(
    http: httpClient,
    adminUrl: "http://localhost:8080/xxl-job-admin",
    appName: "agentscope");

var taskId = await scheduler.ScheduleAsync(new ScheduleAgentTask(
    TaskId: "health-check",
    AgentName: "HealthCheckAgent",
    CronExpression: "0 */5 * * * ?"
));
```

`XxlJobAgentScheduler` registers tasks via the XXL-Job Admin HTTP API. Schedule policies (CRON, concurrency, routing) are configured in the XXL-Job admin console.

## Choosing one

| Scenario | Recommendation |
| --- | --- |
| Local or small cluster, no external scheduler | Quartz |
| Need console, cross-node routing, task logs | XXL-Job |
| Bring your own scheduler | Implement `IAgentScheduler` |
