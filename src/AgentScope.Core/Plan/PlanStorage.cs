// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AgentScope.Core.Plan;

/// <summary>
/// Interface for plan storage.
/// Plan 存储接口
/// </summary>
public interface IPlanStorage
{
    /// <summary>
    /// Saves a plan.
    /// </summary>
    Task SaveAsync(Plan plan);

    /// <summary>
    /// Loads a plan by ID.
    /// </summary>
    Task<Plan?> LoadAsync(string planId);

    /// <summary>
    /// Deletes a plan.
    /// </summary>
    Task<bool> DeleteAsync(string planId);

    /// <summary>
    /// Lists all stored plan IDs.
    /// </summary>
    Task<IReadOnlyList<string>> ListAsync();

    /// <summary>
    /// Checks if a plan exists.
    /// </summary>
    Task<bool> ExistsAsync(string planId);
}

/// <summary>
/// JSON file-based plan storage.
/// JSON 文件 Plan 存储
/// </summary>
public class JsonFilePlanStorage : IPlanStorage
{
    private readonly string _baseDirectory;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public JsonFilePlanStorage(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plans");
        Directory.CreateDirectory(_baseDirectory);
    }

    public Task SaveAsync(Plan plan)
    {
        var filePath = GetFilePath(plan.Id);
        var json = JsonSerializer.Serialize(plan, _jsonOptions);
        File.WriteAllText(filePath, json);
        return Task.CompletedTask;
    }

    public Task<Plan?> LoadAsync(string planId)
    {
        var filePath = GetFilePath(planId);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<Plan?>(null);
        }

        var json = File.ReadAllText(filePath);
        var plan = JsonSerializer.Deserialize<Plan>(json, _jsonOptions);
        return Task.FromResult(plan);
    }

    public Task<bool> DeleteAsync(string planId)
    {
        var filePath = GetFilePath(planId);
        if (!File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        File.Delete(filePath);
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<string>> ListAsync()
    {
        var files = Directory.GetFiles(_baseDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .Cast<string>()
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    public Task<bool> ExistsAsync(string planId)
    {
        var filePath = GetFilePath(planId);
        return Task.FromResult(File.Exists(filePath));
    }

    private string GetFilePath(string planId)
    {
        return Path.Combine(_baseDirectory, $"{planId}.json");
    }
}

/// <summary>
/// In-memory plan storage (for testing).
/// 内存 Plan 存储
/// </summary>
public class InMemoryPlanStorage : IPlanStorage
{
    private readonly Dictionary<string, Plan> _plans = new();

    public Task SaveAsync(Plan plan)
    {
        _plans[plan.Id] = plan;
        return Task.CompletedTask;
    }

    public Task<Plan?> LoadAsync(string planId)
    {
        _plans.TryGetValue(planId, out var plan);
        return Task.FromResult(plan);
    }

    public Task<bool> DeleteAsync(string planId)
    {
        return Task.FromResult(_plans.Remove(planId));
    }

    public Task<IReadOnlyList<string>> ListAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>(_plans.Keys.ToList());
    }

    public Task<bool> ExistsAsync(string planId)
    {
        return Task.FromResult(_plans.ContainsKey(planId));
    }
}

/// <summary>
/// Plan manager with storage support.
/// 带存储支持的 Plan 管理器
/// </summary>
public class PlanManager
{
    private readonly IPlanStorage _storage;
    private readonly Dictionary<string, Plan> _cache = new();

    public PlanManager(IPlanStorage? storage = null)
    {
        _storage = storage ?? new InMemoryPlanStorage();
    }

    /// <summary>
    /// Creates and saves a new plan.
    /// </summary>
    public async Task<Plan> CreatePlanAsync(string name, string? description = null)
    {
        var plan = new Plan
        {
            Name = name,
            Description = description,
            RootNode = new PlanNode
            {
                Name = name,
                Type = PlanNodeType.Sequential,
                Description = description
            }
        };

        await _storage.SaveAsync(plan);
        _cache[plan.Id] = plan;

        return plan;
    }

    /// <summary>
    /// Loads a plan from storage.
    /// </summary>
    public async Task<Plan?> GetPlanAsync(string planId)
    {
        if (_cache.TryGetValue(planId, out var cached))
        {
            return cached;
        }

        var plan = await _storage.LoadAsync(planId);
        if (plan != null)
        {
            _cache[planId] = plan;
        }

        return plan;
    }

    /// <summary>
    /// Saves a plan to storage.
    /// </summary>
    public async Task SavePlanAsync(Plan plan)
    {
        plan.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveAsync(plan);
        _cache[plan.Id] = plan;
    }

    /// <summary>
    /// Deletes a plan.
    /// </summary>
    public async Task<bool> DeletePlanAsync(string planId)
    {
        _cache.Remove(planId);
        return await _storage.DeleteAsync(planId);
    }

    /// <summary>
    /// Lists all plans.
    /// </summary>
    public async Task<IReadOnlyList<Plan>> ListPlansAsync()
    {
        var ids = await _storage.ListAsync();
        var plans = new List<Plan>();

        foreach (var id in ids)
        {
            var plan = await GetPlanAsync(id);
            if (plan != null)
            {
                plans.Add(plan);
            }
        }

        return plans;
    }

    /// <summary>
    /// Imports a plan from JSON.
    /// </summary>
    public async Task<Plan> ImportFromJsonAsync(string json)
    {
        var plan = JsonSerializer.Deserialize<Plan>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (plan == null)
        {
            throw new ArgumentException("Invalid JSON", nameof(json));
        }

        // Generate new ID to avoid conflicts
        plan.Id = Guid.NewGuid().ToString();
        
        await _storage.SaveAsync(plan);
        _cache[plan.Id] = plan;

        return plan;
    }

    /// <summary>
    /// Exports a plan to JSON.
    /// </summary>
    public async Task<string> ExportToJsonAsync(string planId)
    {
        var plan = await GetPlanAsync(planId);
        if (plan == null)
        {
            throw new ArgumentException($"Plan {planId} not found", nameof(planId));
        }

        return JsonSerializer.Serialize(plan, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
