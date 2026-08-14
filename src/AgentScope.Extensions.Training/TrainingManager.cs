using System.Net.Http.Json;
using System.Text.Json;

namespace AgentScope.Extensions.Training;

public sealed class TrainingManager
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public TrainingManager(HttpClient http, string baseUrl)
    {
        _http = http;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> StartTrainingAsync(string modelName, string dataset, TrainingConfig? config = null, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync($"{_baseUrl}/api/training/jobs", new
        {
            model_name = modelName,
            dataset,
            config
        }, ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("job_id").GetString() ?? "";
    }

    public async Task<TrainingStatus> GetStatusAsync(string jobId, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"{_baseUrl}/api/training/jobs/{jobId}", ct);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
        return new TrainingStatus(
            json.GetProperty("status").GetString() ?? "unknown",
            json.GetProperty("progress").GetDouble(),
            json.GetProperty("metrics").GetProperty("loss").GetDouble());
    }

    public async Task CancelTrainingAsync(string jobId, CancellationToken ct = default)
    {
        var resp = await _http.PostAsync($"{_baseUrl}/api/training/jobs/{jobId}/cancel", null, ct);
        resp.EnsureSuccessStatusCode();
    }
}

public sealed record TrainingConfig(int Epochs = 3, double LearningRate = 1e-5, int? BatchSize = null);
public sealed record TrainingStatus(string Status, double Progress, double CurrentLoss);
