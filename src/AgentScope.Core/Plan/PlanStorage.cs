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
/// Interface for plan persistence storage.
/// 计划持久化存储接口。
/// Provides methods for saving, loading, deleting, and listing plans.
/// 提供保存、加载、删除和列出计划的方法。
/// Corresponds to Java: io.agentscope.core.plan.IPlanStorage
/// 对应 Java: io.agentscope.core.plan.IPlanStorage
/// </summary>
public interface IPlanStorage
{
    /// <summary>
    /// Saves a plan to storage asynchronously.
    /// 异步将计划保存到存储。
    /// </summary>
    /// <param name="plan">The plan to save. 要保存的计划。</param>
    Task SaveAsync(Plan plan);

    /// <summary>
    /// Loads a plan from storage by its ID asynchronously.
    /// 通过 ID 异步从存储加载计划。
    /// </summary>
    /// <param name="planId">The ID of the plan to load. 要加载的计划 ID。</param>
    /// <returns>The loaded plan, or null if not found. 加载的计划，如果未找到则返回 null。</returns>
    Task<Plan?> LoadAsync(string planId);

    /// <summary>
    /// Deletes a plan from storage asynchronously.
    /// 异步从存储删除计划。
    /// </summary>
    /// <param name="planId">The ID of the plan to delete. 要删除的计划 ID。</param>
    /// <returns>True if the plan was found and deleted; otherwise false.
    /// 如果找到并删除了计划则返回 true；否则返回 false。</returns>
    Task<bool> DeleteAsync(string planId);

    /// <summary>
    /// Lists all stored plan IDs asynchronously.
    /// 异步列出所有已存储的计划 ID。
    /// </summary>
    /// <returns>A read-only list of plan IDs. 计划 ID 的只读列表。</returns>
    Task<IReadOnlyList<string>> ListAsync();

    /// <summary>
    /// Checks if a plan exists in storage asynchronously.
    /// 异步检查计划是否存在于存储中。
    /// </summary>
    /// <param name="planId">The ID of the plan to check. 要检查的计划 ID。</param>
    /// <returns>True if the plan exists; otherwise false. 如果计划存在则返回 true；否则返回 false。</returns>
    Task<bool> ExistsAsync(string planId);
}

/// <summary>
/// JSON file-based implementation of plan storage.
/// 基于 JSON 文件的计划存储实现。
/// Plans are serialized to individual JSON files in a specified directory.
/// 计划被序列化为指定目录中的单个 JSON 文件。
/// Corresponds to Java: io.agentscope.core.plan.JsonFilePlanStorage
/// 对应 Java: io.agentscope.core.plan.JsonFilePlanStorage
/// </summary>
public class JsonFilePlanStorage : IPlanStorage
{
    /// <summary>
    /// Base directory where plan JSON files are stored.
    /// 存储计划 JSON 文件的基础目录。
    /// </summary>
    private readonly string _baseDirectory;

    /// <summary>
    /// JSON serializer options for plan serialization/deserialization.
    /// 用于计划序列化/反序列化的 JSON 序列化器选项。
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Initializes a new instance of JsonFilePlanStorage.
    /// 初始化 JsonFilePlanStorage 的新实例。
    /// </summary>
    /// <param name="baseDirectory">Optional base directory path. Defaults to a "plans" subdirectory under the app base directory.
    /// 可选的基础目录路径。默认为应用程序基础目录下的 "plans" 子目录。</param>
    public JsonFilePlanStorage(string? baseDirectory = null)
    {
        _baseDirectory = baseDirectory ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plans");
        Directory.CreateDirectory(_baseDirectory);
    }

    /// <summary>
    /// Saves a plan to a JSON file asynchronously.
    /// 异步将计划保存到 JSON 文件。
    /// </summary>
    /// <param name="plan">The plan to save. 要保存的计划。</param>
    public Task SaveAsync(Plan plan)
    {
        var filePath = GetFilePath(plan.Id);
        var json = JsonSerializer.Serialize(plan, _jsonOptions);
        File.WriteAllText(filePath, json);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads a plan from a JSON file by its ID asynchronously.
    /// 通过 ID 异步从 JSON 文件加载计划。
    /// </summary>
    /// <param name="planId">The ID of the plan to load. 要加载的计划 ID。</param>
    /// <returns>The loaded plan, or null if the file does not exist.
    /// 加载的计划，如果文件不存在则返回 null。</returns>
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

    /// <summary>
    /// Deletes a plan's JSON file asynchronously.
    /// 异步删除计划的 JSON 文件。
    /// </summary>
    /// <param name="planId">The ID of the plan to delete. 要删除的计划 ID。</param>
    /// <returns>True if the file was found and deleted; otherwise false.
    /// 如果找到并删除了文件则返回 true；否则返回 false。</returns>
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

    /// <summary>
    /// Lists all stored plan IDs by scanning JSON files in the base directory.
    /// 通过扫描基础目录中的 JSON 文件列出所有已存储的计划 ID。
    /// </summary>
    /// <returns>A read-only list of plan IDs. 计划 ID 的只读列表。</returns>
    public Task<IReadOnlyList<string>> ListAsync()
    {
        var files = Directory.GetFiles(_baseDirectory, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .Cast<string>()
            .ToList();
        return Task.FromResult<IReadOnlyList<string>>(files);
    }

    /// <summary>
    /// Checks if a plan's JSON file exists asynchronously.
    /// 异步检查计划的 JSON 文件是否存在。
    /// </summary>
    /// <param name="planId">The ID of the plan to check. 要检查的计划 ID。</param>
    /// <returns>True if the file exists; otherwise false. 如果文件存在则返回 true；否则返回 false。</returns>
    public Task<bool> ExistsAsync(string planId)
    {
        var filePath = GetFilePath(planId);
        return Task.FromResult(File.Exists(filePath));
    }

    /// <summary>
    /// Gets the full file path for a plan ID.
    /// 获取计划 ID 的完整文件路径。
    /// </summary>
    /// <param name="planId">The plan ID. 计划 ID。</param>
    /// <returns>The full file path with .json extension. 带有 .json 扩展名的完整文件路径。</returns>
    private string GetFilePath(string planId)
    {
        return Path.Combine(_baseDirectory, $"{planId}.json");
    }
}

/// <summary>
/// In-memory implementation of plan storage, useful for testing and transient scenarios.
/// 计划存储的内存实现，适用于测试和临时场景。
/// Corresponds to Java: io.agentscope.core.plan.InMemoryPlanStorage
/// 对应 Java: io.agentscope.core.plan.InMemoryPlanStorage
/// </summary>
public class InMemoryPlanStorage : IPlanStorage
{
    /// <summary>
    /// Internal dictionary storing plans in memory, keyed by plan ID.
    /// 在内存中存储计划的内部字典，以计划 ID 为键。
    /// </summary>
    private readonly Dictionary<string, Plan> _plans = new();

    /// <summary>
    /// Saves a plan to the in-memory dictionary.
    /// 将计划保存到内存字典中。
    /// </summary>
    /// <param name="plan">The plan to save. 要保存的计划。</param>
    public Task SaveAsync(Plan plan)
    {
        _plans[plan.Id] = plan;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Loads a plan from the in-memory dictionary by its ID.
    /// 通过 ID 从内存字典加载计划。
    /// </summary>
    /// <param name="planId">The ID of the plan to load. 要加载的计划 ID。</param>
    /// <returns>The loaded plan, or null if not found. 加载的计划，如果未找到则返回 null。</returns>
    public Task<Plan?> LoadAsync(string planId)
    {
        _plans.TryGetValue(planId, out var plan);
        return Task.FromResult(plan);
    }

    /// <summary>
    /// Deletes a plan from the in-memory dictionary.
    /// 从内存字典中删除计划。
    /// </summary>
    /// <param name="planId">The ID of the plan to delete. 要删除的计划 ID。</param>
    /// <returns>True if the plan was found and removed; otherwise false.
    /// 如果找到并移除了计划则返回 true；否则返回 false。</returns>
    public Task<bool> DeleteAsync(string planId)
    {
        return Task.FromResult(_plans.Remove(planId));
    }

    /// <summary>
    /// Lists all plan IDs stored in memory.
    /// 列出内存中存储的所有计划 ID。
    /// </summary>
    /// <returns>A read-only list of plan IDs. 计划 ID 的只读列表。</returns>
    public Task<IReadOnlyList<string>> ListAsync()
    {
        return Task.FromResult<IReadOnlyList<string>>(_plans.Keys.ToList());
    }

    /// <summary>
    /// Checks if a plan exists in the in-memory dictionary.
    /// 检查计划是否存在于内存字典中。
    /// </summary>
    /// <param name="planId">The ID of the plan to check. 要检查的计划 ID。</param>
    /// <returns>True if the plan exists; otherwise false. 如果计划存在则返回 true；否则返回 false。</returns>
    public Task<bool> ExistsAsync(string planId)
    {
        return Task.FromResult(_plans.ContainsKey(planId));
    }
}

/// <summary>
/// Plan manager providing high-level CRUD operations with caching and storage support.
/// 计划管理器，提供带有缓存和存储支持的高级 CRUD 操作。
/// Manages the full lifecycle of plans including creation, retrieval, persistence, import/export.
/// 管理计划的完整生命周期，包括创建、检索、持久化、导入/导出。
/// Corresponds to Java: io.agentscope.core.plan.PlanManager
/// 对应 Java: io.agentscope.core.plan.PlanManager
/// </summary>
public class PlanManager
{
    /// <summary>
    /// The underlying storage implementation for plan persistence.
    /// 用于计划持久化的底层存储实现。
    /// </summary>
    private readonly IPlanStorage _storage;

    /// <summary>
    /// In-memory cache of loaded plans for fast access.
    /// 已加载计划的内存缓存，用于快速访问。
    /// </summary>
    private readonly Dictionary<string, Plan> _cache = new();

    /// <summary>
    /// Initializes a new instance of PlanManager with optional storage.
    /// 使用可选的存储初始化 PlanManager 的新实例。
    /// </summary>
    /// <param name="storage">Optional storage implementation. Defaults to InMemoryPlanStorage.
    /// 可选的存储实现。默认为 InMemoryPlanStorage。</param>
    public PlanManager(IPlanStorage? storage = null)
    {
        _storage = storage ?? new InMemoryPlanStorage();
    }

    /// <summary>
    /// Creates a new plan, saves it to storage, and caches it.
    /// 创建新计划，保存到存储并缓存。
    /// </summary>
    /// <param name="name">The name of the plan. 计划名称。</param>
    /// <param name="description">Optional description of the plan. 计划的可选描述。</param>
    /// <returns>The newly created Plan instance. 新创建的 Plan 实例。</returns>
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
    /// Retrieves a plan by ID, checking the cache first before loading from storage.
    /// 通过 ID 检索计划，先检查缓存，再加载自存储。
    /// </summary>
    /// <param name="planId">The ID of the plan to retrieve. 要检索的计划 ID。</param>
    /// <returns>The plan if found; otherwise null. 如果找到则返回计划；否则返回 null。</returns>
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
    /// Saves a plan to storage and updates the cache.
    /// 将计划保存到存储并更新缓存。
    /// </summary>
    /// <param name="plan">The plan to save. 要保存的计划。</param>
    public async Task SavePlanAsync(Plan plan)
    {
        plan.UpdatedAt = DateTime.UtcNow;
        await _storage.SaveAsync(plan);
        _cache[plan.Id] = plan;
    }

    /// <summary>
    /// Deletes a plan from both cache and storage.
    /// 从缓存和存储中删除计划。
    /// </summary>
    /// <param name="planId">The ID of the plan to delete. 要删除的计划 ID。</param>
    /// <returns>True if the plan was found and deleted; otherwise false.
    /// 如果找到并删除了计划则返回 true；否则返回 false。</returns>
    public async Task<bool> DeletePlanAsync(string planId)
    {
        _cache.Remove(planId);
        return await _storage.DeleteAsync(planId);
    }

    /// <summary>
    /// Lists all plans by loading them from storage.
    /// 通过从存储加载列出所有计划。
    /// </summary>
    /// <returns>A read-only list of all plans. 所有计划的只读列表。</returns>
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
    /// Imports a plan from a JSON string, generating a new ID to avoid conflicts.
    /// 从 JSON 字符串导入计划，生成新 ID 以避免冲突。
    /// </summary>
    /// <param name="json">The JSON string representing the plan. 表示计划的 JSON 字符串。</param>
    /// <returns>The imported Plan instance. 导入的 Plan 实例。</returns>
    /// <exception cref="ArgumentException">Thrown if the JSON is invalid or null.
    /// 如果 JSON 无效或为 null 则抛出。</exception>
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

        // Generate a new ID to avoid conflicts with existing plans
        // 生成新 ID 以避免与现有计划冲突
        plan.Id = Guid.NewGuid().ToString();
        
        await _storage.SaveAsync(plan);
        _cache[plan.Id] = plan;

        return plan;
    }

    /// <summary>
    /// Exports a plan to a JSON string.
    /// 将计划导出为 JSON 字符串。
    /// </summary>
    /// <param name="planId">The ID of the plan to export. 要导出的计划 ID。</param>
    /// <returns>A JSON string representation of the plan. 计划的 JSON 字符串表示。</returns>
    /// <exception cref="ArgumentException">Thrown if the plan is not found.
    /// 如果未找到计划则抛出。</exception>
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
