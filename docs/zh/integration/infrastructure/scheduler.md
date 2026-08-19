# Scheduler（定时调度）

`AgentScope.Extensions.Scheduler` 让 Agent 可以按调度器配置周期性执行——比如"每天 8 点跑一次日报 Agent"、"每 5 秒做一次健康巡检"。模块抽出统一的 `AgentScheduler` 接口，提供两个实现：

| 子模块 | 实现 | 部署形态 |
| --- | --- | --- |
| `AgentScope.Extensions.Scheduler.Quartz` | [Quartz](https://www.quartz-scheduler.org/) | 单机或集群（共享 Quartz 数据库） |
| `AgentScope.Extensions.Scheduler.XxlJob` | [XXL-Job](https://www.xuxueli.com/xxl-job/) | 分布式调度，依赖 admin server |

底层 SPI 在 `AgentScope.Extensions.Scheduler.Common`，可以自己实现接入其他调度框架。

## 共享概念

- `AgentConfig`（或 `RuntimeAgentConfig`）：定义 Agent 怎么"造"出来——名字、模型配置、system prompt、工具集等。
- `ScheduleConfig`：定义调度策略——CRON、固定速率、最大并发等。
- `AgentScheduler`：核心接口，`Schedule(...)` / `Pause(...)` / `Resume(...)` / `Cancel(...)` / `Shutdown()`。
- `ScheduleAgentTask`：一次注册返回的"被调度的 Agent 任务"句柄。

每次任务触发时，调度器会"现造"一个新的 Agent 实例并执行，避免状态串台。

## Quartz 模式（单机/集群）

### 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Scheduler.Quartz" Version="$(AgentScopeVersion)" />
```

### 用法

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
    .SysPrompt("你是日报助手，请每天生成销售汇总")
    .Build();

ScheduleConfig schedule = ScheduleConfig.Builder()
    .ScheduleMode(ScheduleMode.FIXED_RATE)
    .FixedRate(5000L)   // 每 5 秒
    // 或 .ScheduleMode(ScheduleMode.CRON).Cron("0 0 8 * * ?")
    .Build();

scheduler.Schedule(agent, schedule);
```

支持运行时管控：

```csharp
scheduler.Pause("DailyReportAgent");
scheduler.Resume("DailyReportAgent");
scheduler.Cancel("DailyReportAgent");
scheduler.Shutdown();
```

## XXL-Job 模式（分布式）

### 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Scheduler.XxlJob" Version="$(AgentScopeVersion)" />
```

### 用法

```csharp
using AgentScope.Extensions.Scheduler.XxlJob;

// 1) 启动 XXL-Job Executor
XxlJobSpringExecutor executor = new();
executor.SetAdminAddresses("http://localhost:8080/xxl-job-admin");
executor.SetAppname("agentscope-demo");
executor.SetAccessToken("xxxxxxxx");
executor.SetPort(9999);
executor.Start();

// 2) 把它包成 AgentScheduler
AgentScheduler scheduler = new XxlJobAgentScheduler(executor);

// 3) 注册一个 Agent 作为 JobHandler
ScheduleAgentTask task = scheduler.Schedule(agentConfig, ScheduleConfig.Builder().Build());
```

之后，**调度策略（CRON、并发、路由）在 XXL-Job 控制台配置**，Agent 名 `DailyReportAgent` 会作为 JobHandler 显示。

## 工具绑定

绑定 Toolkit 时使用 `RuntimeAgentConfig`（过渡 API，未来可能调整）：

```csharp
RuntimeAgentConfig agent = RuntimeAgentConfig.Builder()
    .Name("OpsAgent")
    .ModelConfig(modelConfig)
    .SysPrompt("巡检并发送告警")
    .Toolkit(toolkit)
    .Build();
```

## 选型建议

- **本地或小集群、不想引入外部调度服务** → Quartz
- **需要可视化控制台、跨节点路由、任务日志** → XXL-Job
- **想接其他调度框架** → 自己实现 `AgentScheduler` 接口
