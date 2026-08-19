# 在线训练（Training）

`AgentScope.Extensions.Training` 在 AgentScope 之上接入 Trinity 训练后端：把生产流量按策略采样、收集 trace、计算奖励，再周期性提交训练，形成闭环。

## 何时使用

- 已有 Trinity（或兼容服务）作为模型训练后端。
- 想把线上流量当训练数据，做强化学习或在线微调。
- 不想改业务调用代码，希望训练流水线对 Agent 调用方"透明"。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Training" Version="$(AgentScopeVersion)" />
```

## 快速上手

```csharp
using AgentScope.Core.Training.Runner;
using AgentScope.Core.Training.Strategy;

TrainingRunner runner = TrainingRunner.Builder()
    .TrinityEndpoint("http://localhost:8080")
    .ModelName("/path/to/model")
    .SelectionStrategy(SamplingRateStrategy.Of(0.1))   // 10% 流量进训练
    .RewardCalculator(agent => 0.0)                    // 自定义奖励
    .CommitIntervalSeconds(300)                        // 每 5 分钟 commit 一次
    .Build();

runner.Start();          // 拦截 Agent，开始采样

// 业务侧照常使用 Agent，无须感知 runner
agent.Call(msg).Wait();

runner.Stop();           // 停止训练流水线
```

## 选样策略

- `SamplingRateStrategy.Of(0.1)`：按比例随机采样。
- `ExplicitMarkingStrategy.Create()`：完全由调用方显式标记哪些请求要进训练。
- 也可以实现 `ITrainingSelectionStrategy` 自定义。

## 奖励计算

`RewardCalculator` 是一个 `Func<AgentBase, double>`，每次采样产生 trajectory 后调用。可以是：

- Lambda：基于回答长度、工具调用次数等启发式打分。
- 自定义类：实现 `IRewardCalculator` 接口，封装更复杂的指标。

```csharp
TrainingRunner runner = TrainingRunner.Builder()
    .TrinityEndpoint(endpoint)
    .ModelName(model)
    .SelectionStrategy(SamplingRateStrategy.Of(0.1))
    .RewardCalculator(new MyMetricRewardCalculator())
    .Build();
```

## 工作机制

1. `runner.Start()` 之后，Agent 的请求会经 `TrainingRouter` 路由：
   - 命中采样 → 调用替换为 Trinity 后端，trace 数据被收集；
   - 未命中 → 走原有模型，无副作用。
2. 命中样本会调 reward calculator 算分，再通过 `TrinityClient.Feedback(...)` 反馈。
3. `commitIntervalSeconds` 周期到达时调用 `Commit(...)`，触发训练任务。

`runner.Stop()` 时会优雅关闭定时器与连接池。

## 关键配置

| 字段 | 说明 |
| --- | --- |
| `TrinityEndpoint` | Trinity 服务地址 |
| `ModelName` | 训练目标模型路径或别名 |
| `SelectionStrategy` | 采样策略 |
| `RewardCalculator` | 奖励计算函数 |
| `CommitIntervalSeconds` | commit 周期，默认 300 |

## 与 Studio 配合

可以同时挂载 `StudioMessageHook`，在 Studio 上实时看到哪些会话被采样进训练；reward 也可以写入 Studio 用于可视化分析。
