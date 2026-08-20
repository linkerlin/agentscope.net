# Online Training

`AgentScope.Extensions.Training` provides `TrainingManager` for managing model fine-tuning tasks via HTTP API.

## When to use

- You have a compatible training backend and want to submit training jobs over REST.
- You need to control training lifecycle (start, query, cancel) from code.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Training" Version="2.0.1" />
```

## TrainingManager

```csharp
using AgentScope.Extensions.Training;

var manager = new TrainingManager(
    http: httpClient,
    baseUrl: "http://localhost:8080");

// Start training
var jobId = await manager.StartTrainingAsync(
    modelName: "my-model",
    dataset: "production-traces",
    config: new TrainingConfig(
        Epochs: 3,
        LearningRate: 1e-5,
        BatchSize: 32));

// Query status
var status = await manager.GetStatusAsync(jobId);
Console.WriteLine($"Status: {status.Status}, Progress: {status.Progress}, Loss: {status.CurrentLoss}");

// Cancel
await manager.CancelTrainingAsync(jobId);
```

### API

| Constructor | Description |
| --- | --- |
| `TrainingManager(HttpClient http, string baseUrl)` | Connect to the training backend |

| Method | Description |
| --- | --- |
| `StartTrainingAsync(string modelName, string dataset, TrainingConfig? config, CancellationToken ct)` | Submit a training job; returns `job_id` |
| `GetStatusAsync(string jobId, CancellationToken ct)` | Query training progress |
| `CancelTrainingAsync(string jobId, CancellationToken ct)` | Cancel a training job |

### Data Models

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

`TrainingManager` is a pure HTTP client — it does not intercept Agent execution. You decide when and how to send production traffic to the training backend.
