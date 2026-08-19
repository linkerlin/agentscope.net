# Scheduler

`AgentScope.Extensions.Scheduler` runs Agents periodically — e.g. "every day at 8 AM run the daily-report Agent" or "every 5 seconds run a health-check Agent". The module abstracts a unified `AgentScheduler` interface and ships two implementations:

| Sub-module | Implementation | Deployment |
| --- | --- | --- |
| `AgentScope.Extensions.Scheduler.Quartz` | [Quartz](https://www.quartz-scheduler.org/) | Standalone or clustered (shared Quartz DB) |
| `AgentScope.Extensions.Scheduler.XxlJob` | [XXL-Job](https://www.xuxueli.com/xxl-job/) | Distributed scheduling, requires admin server |

The SPI lives in `AgentScope.Extensions.Scheduler.Common` so you can plug in other schedulers.

## Shared concepts

- `AgentConfig` (or `RuntimeAgentConfig`): how to construct the Agent — name, model config, system prompt, toolkit.
- `ScheduleConfig`: scheduling policy — CRON, fixed rate, max parallelism.
- `AgentScheduler`: the core interface — `Schedule(...)` / `Pause(...)` / `Resume(...)` / `Cancel(...)` / `Shutdown()`.
- `ScheduleAgentTask`: handle returned from a registration; represents one scheduled task.

Each trigger creates a fresh Agent instance for execution to avoid leaked state.

## Quartz mode

### Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Scheduler.Quartz" Version="$(AgentScopeVersion)" />
```

### Usage

```csharp
using AgentScope.Extensions.Scheduler;
using AgentScope.Extensions.Scheduler.Config;
using AgentScope.Extensions.Scheduler.Quartz;

AgentScheduler scheduler = QuartzAgentScheduler.Builder()
    .AutoStart(true)
    .Build();

AgentConfig agent = AgentConfig.Builder()
    .Name("DailyReportAgent")
    .ModelConfig(DashScopeModelConfig.Builder()
        .ApiKey(apiKey).ModelName("qwen-plus").Build())
    .SysPrompt("You are a report assistant; please generate a sales summary every day.")
    .Build();

ScheduleConfig schedule = ScheduleConfig.Builder()
    .ScheduleMode(ScheduleMode.FIXED_RATE)
    .FixedRate(5000L)   // every 5s
    // or .ScheduleMode(ScheduleMode.CRON).Cron("0 0 8 * * ?")
    .Build();

scheduler.Schedule(agent, schedule);
```

Runtime control:

```csharp
scheduler.Pause("DailyReportAgent");
scheduler.Resume("DailyReportAgent");
scheduler.Cancel("DailyReportAgent");
scheduler.Shutdown();
```

## XXL-Job mode

### Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Scheduler.XxlJob" Version="$(AgentScopeVersion)" />
```

### Usage

```csharp
using AgentScope.Extensions.Scheduler.XxlJob;

// 1) Boot the XXL-Job executor
XxlJobSpringExecutor executor = new();
executor.SetAdminAddresses("http://localhost:8080/xxl-job-admin");
executor.SetAppname("agentscope-demo");
executor.SetAccessToken("xxxxxxxx");
executor.SetPort(9999);
executor.Start();

// 2) Wrap it as AgentScheduler
AgentScheduler scheduler = new XxlJobAgentScheduler(executor);

// 3) Register an Agent as a JobHandler
ScheduleAgentTask task = scheduler.Schedule(agentConfig, ScheduleConfig.Builder().Build());
```

After registration, **configure the schedule (CRON, parallelism, routing) in the XXL-Job admin console**. The Agent name `DailyReportAgent` shows up there as the JobHandler.

## Binding tools

Use `RuntimeAgentConfig` (a transitional API that may evolve) when you need to bind a Toolkit:

```csharp
RuntimeAgentConfig agent = RuntimeAgentConfig.Builder()
    .Name("OpsAgent")
    .ModelConfig(modelConfig)
    .SysPrompt("Run health checks and send alerts.")
    .Toolkit(toolkit)
    .Build();
```

## Choosing one

- **Local or small cluster, no external scheduler service** → Quartz
- **Need a console, cross-node routing, task logs** → XXL-Job
- **Bring your own scheduler framework** → implement the `AgentScheduler` SPI
