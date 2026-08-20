# 在线训练（Training）

`AgentScope.Extensions.Training` 提供 `TrainingManager` 客户端，用于通过 HTTP API 管理模型微调训练任务。

## 何时使用

- 已有兼容的训练后端服务，希望通过 REST API 提交训练作业。
- 需要在代码中控制训练任务的启停与状态查询。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Training" Version="2.0.1" />
```

## TrainingManager

```csharp
using AgentScope.Extensions.Training;

var manager = new TrainingManager(
    http: httpClient,
    baseUrl: "http://localhost:8080");

// 启动训练
var jobId = await manager.StartTrainingAsync(
    modelName: "my-model",
    dataset: "production-traces",
    config: new TrainingConfig(
        Epochs: 3,
        LearningRate: 1e-5,
        BatchSize: 32));

// 查询状态
var status = await manager.GetStatusAsync(jobId);
Console.WriteLine($"状态: {status.Status}, 进度: {status.Progress}, Loss: {status.CurrentLoss}");

// 取消训练
await manager.CancelTrainingAsync(jobId);
```

### API

| 构造方法 | 说明 |
| --- | --- |
| `TrainingManager(HttpClient http, string baseUrl)` | 连接训练后端服务 |

| 方法 | 说明 |
| --- | --- |
| `StartTrainingAsync(string modelName, string dataset, TrainingConfig? config, CancellationToken ct)` | 提交训练任务，返回 `job_id` |
| `GetStatusAsync(string jobId, CancellationToken ct)` | 查询训练进度 |
| `CancelTrainingAsync(string jobId, CancellationToken ct)` | 取消训练任务 |

### 数据模型

```csharp
public sealed record TrainingConfig(
    int Epochs = 3,
    double LearningRate = 1e-5,
    int? BatchSize = null);

public sealed record TrainingStatus(
    string Status,
    double Progress,
    double CurrentLoss);
```

## 工作机制

`TrainingManager` 仅负责 HTTP 客户端调用，不干预 Agent 的执行过程。你可以在业务代码中自行决定何时将生产流量发送到训练后端，实现"采集 → 微调 → 部署"的闭环。
