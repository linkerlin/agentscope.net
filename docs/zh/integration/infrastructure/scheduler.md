# Scheduler（定时调度）

`AgentScope.Extensions.Scheduler` 让 Agent 可以按调度器配置周期性执行。模块抽出统一接口 `IAgentScheduler`，提供两个实现：

| 子模块 | 实现 | 部署形态 |
| --- | --- | --- |
| `AgentScope.Extensions.Scheduler.Quartz` | [Quartz.NET](https://www.quartz-scheduler.net/) | 单机或集群 |
| `AgentScope.Extensions.Scheduler.XxlJob` | [XXL-Job](https://www.xuxueli.com/xxl-job/) | 分布式，依赖 Admin Server |

## 公共接口

### IAgentScheduler

| 方法 | 说明 |
| --- | --- |
| `Task<string> ScheduleAsync(ScheduleAgentTask task, CancellationToken ct)` | 注册一个定时 Agent 任务，返回任务 ID |
| `Task CancelAsync(string taskId, CancellationToken ct)` | 取消已注册的任务 |
| `Task<IReadOnlyList<ScheduleAgentTask>> ListTasksAsync(CancellationToken ct)` | 列举所有已注册任务 |

### ScheduleAgentTask

```csharp
public sealed record ScheduleAgentTask(
    string TaskId,
    string AgentName,
    string CronExpression,
    IDictionary<string, object>? InputParams = null);
```

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Scheduler.Quartz" Version="2.0.1" />
<!-- 或 -->
<PackageReference Include="AgentScope.Extensions.Scheduler.XxlJob" Version="2.0.1" />
```

## Quartz 模式

```csharp
using AgentScope.Extensions.Scheduler;
using AgentScope.Extensions.Scheduler.Quartz;

var scheduler = new QuartzAgentScheduler(
    agentResolver: name => agentFactory(name));

var taskId = await scheduler.ScheduleAsync(new ScheduleAgentTask(
    TaskId: "daily-report",
    AgentName: "DailyReportAgent",
    CronExpression: "0 0 8 * * ?",
    InputParams: new Dictionary<string, object> { ["text"] = "生成销售日报" }
));

// 运行时管控
await scheduler.CancelAsync("daily-report");
var tasks = await scheduler.ListTasksAsync();
```

`QuartzAgentScheduler` 构造时可选传入 `Func<string, IAgent>? agentResolver`，若提供则每次触发时使用该解析器获取 Agent 实例并执行。

## XXL-Job 模式

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

`XxlJobAgentScheduler` 通过 XXL-Job Admin HTTP API 注册定时任务。调度策略（CRON、并发、路由）在 XXL-Job 控制台配置。

## 选型建议

| 场景 | 推荐 |
| --- | --- |
| 本地或小集群，无需外部调度服务 | Quartz |
| 需要可视化控制台、跨节点路由、任务日志 | XXL-Job |
| 接入其他调度框架 | 实现 `IAgentScheduler` 接口 |
