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
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AgentScope.Core.Message;
using Microsoft.EntityFrameworkCore;

namespace AgentScope.Core.Memory;

/// <summary>
/// Entity for storing messages in SQLite
/// </summary>
public class MessageEntity
{
    public int Id { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Metadata { get; set; }
    public string? Url { get; set; }
}

/// <summary>
/// DbContext for memory storage
/// </summary>
public class MemoryDbContext : DbContext
{
    public DbSet<MessageEntity> Messages { get; set; } = null!;

    private readonly string _connectionString;

    public MemoryDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId);
            entity.HasIndex(e => e.Timestamp);
        });
    }
}

/// <summary>
/// SQLite-based persistent memory implementation.
/// SQLite 持久化内存实现
/// 
/// Performance Notes:
/// - Add() persists immediately for data safety
/// - Use BeginBatch() / EndBatch() for bulk operations (faster)
/// </summary>
public class SqliteMemory : IPersistentMemory, IDisposable, IAsyncDisposable
{
    private readonly MemoryDbContext _dbContext;
    private readonly MemoryBase _cache = new();
    private bool _disposed = false;
    private bool _batchMode = false;
    private readonly object _lock = new();

    /// <summary>
    /// Whether in batch mode (deferred persistence).
    /// </summary>
    public bool IsBatchMode => _batchMode;

    public SqliteMemory(string databasePath)
    {
        var connectionString = $"Data Source={databasePath}";
        _dbContext = new MemoryDbContext(connectionString);
        _dbContext.Database.EnsureCreated();
        
        // Load existing messages from database into cache
        var entities = _dbContext.Messages.OrderBy(m => m.Timestamp).ToList();
        foreach (var entity in entities)
        {
            _cache.Add(EntityToMessage(entity));
        }
    }

    /// <summary>
    /// Begin batch mode - defer persistence until EndBatch().
    /// 开始批量模式 - 延迟持久化直到 EndBatch()
    /// </summary>
    public void BeginBatch()
    {
        _batchMode = true;
    }

    /// <summary>
    /// End batch mode - persist all pending changes.
    /// 结束批量模式 - 持久化所有待处理的更改
    /// </summary>
    public void EndBatch()
    {
        _batchMode = false;
        _dbContext.SaveChanges();
    }

    /// <summary>
    /// End batch mode - persist all pending changes (async).
    /// </summary>
    public async Task EndBatchAsync()
    {
        _batchMode = false;
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Add message to memory and persist to database.
    /// 添加消息到内存并持久化到数据库
    /// </summary>
    public void Add(Msg message)
    {
        lock (_lock)
        {
            _cache.Add(message);
            
            var entity = MessageToEntity(message);
            _dbContext.Messages.Add(entity);
            
            // Persist immediately unless in batch mode
            if (!_batchMode)
            {
                _dbContext.SaveChanges();
            }
        }
    }

    public List<Msg> GetAll()
    {
        return _cache.GetAll();
    }

    public List<Msg> GetRecent(int count)
    {
        return _cache.GetRecent(count);
    }

    public void Clear()
    {
        _cache.Clear();
        _dbContext.Messages.RemoveRange(_dbContext.Messages);
        _dbContext.SaveChanges();
    }

    public int Count()
    {
        return _cache.Count();
    }

    public async Task<List<Msg>> SearchAsync(string query, int limit = 10)
    {
        var entities = await _dbContext.Messages
            .Where(m => EF.Functions.Like(m.Content ?? "", $"%{query}%"))
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .ToListAsync();
        
        return entities.Select(EntityToMessage).ToList();
    }

    public Task SaveAsync()
    {
        return _dbContext.SaveChangesAsync();
    }

    public async Task LoadAsync()
    {
        var entities = await _dbContext.Messages.ToListAsync();
        _cache.Clear();
        foreach (var entity in entities)
        {
            _cache.Add(EntityToMessage(entity));
        }
    }

    private static MessageEntity MessageToEntity(Msg message)
    {
        return new MessageEntity
        {
            MessageId = message.Id,
            Name = message.Name,
            Role = message.Role,
            Content = message.Content?.ToString(),
            Timestamp = message.Timestamp,
            Metadata = message.Metadata != null 
                ? JsonSerializer.Serialize(message.Metadata) 
                : null,
            Url = message.Url != null 
                ? JsonSerializer.Serialize(message.Url) 
                : null
        };
    }

    private static Msg EntityToMessage(MessageEntity entity)
    {
        return new Msg
        {
            Id = entity.MessageId,
            Name = entity.Name,
            Role = entity.Role,
            Content = entity.Content,
            Timestamp = entity.Timestamp,
            Metadata = entity.Metadata != null 
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(entity.Metadata) 
                : null,
            Url = entity.Url != null 
                ? JsonSerializer.Deserialize<List<string>>(entity.Url) 
                : null
        };
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Flush pending changes if in batch mode
            if (_batchMode)
            {
                try { _dbContext.SaveChanges(); } catch { }
            }
            _dbContext?.Dispose();
        }

        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_batchMode)
        {
            try { await _dbContext.SaveChangesAsync(); } catch { }
        }
        
        await _dbContext.DisposeAsync();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
