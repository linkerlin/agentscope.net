# Online Training

`AgentScope.Extensions.Training` plugs a Trinity-style training backend into AgentScope: it samples production traffic, collects traces, computes rewards, and periodically commits training jobs — closing the loop.

## When to use

- You run Trinity (or a compatible service) as the training store.
- You want to use live traffic for reinforcement learning or online fine-tuning.
- You want the training pipeline to be transparent to the Agent's callers.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Training" Version="$(AgentScopeVersion)" />
```

## Quickstart

```csharp
using AgentScope.Core.Training.Runner;
using AgentScope.Core.Training.Strategy;

TrainingRunner runner = TrainingRunner.Builder()
    .TrinityEndpoint("http://localhost:8080")
    .ModelName("/path/to/model")
    .SelectionStrategy(SamplingRateStrategy.Of(0.1))   // 10% sampling
    .RewardCalculator(agent => 0.0)                    // custom reward
    .CommitIntervalSeconds(300)                        // commit every 5 minutes
    .Build();

runner.Start();          // intercept Agent calls and start sampling

// Business code keeps using the Agent unmodified
agent.Call(msg).Wait();

runner.Stop();           // stop the training pipeline
```

## Selection strategies

- `SamplingRateStrategy.Of(0.1)`: random sampling at the given rate.
- `ExplicitMarkingStrategy.Create()`: only marked requests are sampled.
- Or implement `TrainingSelectionStrategy` for custom behavior.

## Reward calculation

`RewardCalculator` is a `Func<AgentBase, double>`, invoked once per sampled trajectory:

- A lambda — heuristics like answer length, tool-call count, etc.
- A custom class implementing `IRewardCalculator` for richer metrics.

```csharp
TrainingRunner runner = TrainingRunner.Builder()
    .TrinityEndpoint(endpoint)
    .ModelName(model)
    .SelectionStrategy(SamplingRateStrategy.Of(0.1))
    .RewardCalculator(new MyMetricRewardCalculator())
    .Build();
```

## How it works

1. After `runner.Start()`, requests go through `TrainingRouter`:
   - sampled → routed to the Trinity store, traces collected;
   - not sampled → original model is used, no side effects.
2. Sampled trajectories invoke the reward calculator and feedback through `TrinityClient.Feedback(...)`.
3. Every `commitIntervalSeconds`, `Commit(...)` triggers a training job.

`runner.Stop()` shuts down timers and connection pools cleanly.

## Key configuration

| Field | Notes |
| --- | --- |
| `TrinityEndpoint` | Trinity service URL |
| `ModelName` | Target model path or alias |
| `SelectionStrategy` | Sampling strategy |
| `RewardCalculator` | Reward function |
| `CommitIntervalSeconds` | Commit interval, default 300 |

## Pairs well with Studio

Attach `StudioMessageHook` simultaneously and you can see in Studio which sessions get sampled and how rewards were computed.
