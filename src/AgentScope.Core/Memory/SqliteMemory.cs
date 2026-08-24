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
/// Entity for storing messages in SQLite database.
/// 用于在 SQLite 数据库中存储消息的实体。
/// Corresponds to Java: io.agentscope.memory.SqliteMemory.MessageEntity
/// </summary>
public class MessageEntity
{
    /// <summary>
    /// Auto-generated primary key.
    /// 自动生成的主键。
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Unique message identifier.
    /// 唯一消息标识符。
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Optional sender name.
    /// 可选的发送者名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Message role (user, assistant, system, tool).
    /// 消息角色（user、assistant、system、tool）。
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Message content as string.
    /// 消息内容（字符串形式）。
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Message timestamp.
    /// 消息时间戳。
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Serialized metadata dictionary (JSON).
    /// 序列化的元数据字典（JSON）。
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Serialized URL list (JSON).
    /// 序列化的 URL 列表（JSON）。
    /// </summary>
    public string? Url { get; set; }
}

/// <summary>
/// DbContext for memory storage using Entity Framework Core with SQLite.
/// 使用 Entity Framework Core 和 SQLite 的记忆存储 DbContext。
/// Corresponds to Java: io.agentscope.memory.SqliteMemory.MemoryDbContext
/// </summary>
public class MemoryDbContext : DbContext
{
    /// <summary>
    /// Messages table.
    /// 消息表。
    /// </summary>
    public DbSet<MessageEntity> Messages { get; set; } = null!;

    /// <summary>
    /// SQLite connection string.
    /// SQLite 连接字符串。
    /// </summary>
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of MemoryDbContext.
    /// 初始化 MemoryDbContext 的新实例。
    /// </summary>
    /// <param name="connectionString">SQLite connection string. / SQLite 连接字符串。</param>
    public MemoryDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite(_connectionString);
    }

    /// <inheritdoc />
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
/// SQLite 持久化内存实现。
/// Corresponds to Java: io.agentscope.memory.SqliteMemory
///
/// Performance Notes:
/// - Add() persists immediately for data safety
/// - Use BeginBatch() / EndBatch() for bulk operations (faster)
///
/// 性能说明：
/// - Add() 立即持久化以保证数据安全
/// - 使用 BeginBatch() / EndBatch() 进行批量操作（更快）
/// </summary>
public class SqliteMemory : IPersistentMemory, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Entity Framework database context.
    /// Entity Framework 数据库上下文。
    /// </summary>
    private readonly MemoryDbContext _dbContext;

    /// <summary>
    /// In-memory cache for fast access.
    /// 用于快速访问的内存缓存。
    /// </summary>
    private readonly MemoryBase _cache = new();

    /// <summary>
    /// Whether this instance has been disposed.
    /// 此实例是否已释放。
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// Whether batch mode is active (deferred persistence).
    /// 是否处于批量模式（延迟持久化）。
    /// </summary>
    private bool _batchMode = false;

    /// <summary>
    /// Lock object for thread safety.
    /// 用于线程安全的锁对象。
    /// </summary>
    private readonly object _lock = new();

    /// <summary>
    /// Whether in batch mode (deferred persistence).
    /// 是否处于批量模式（延迟持久化）。
    /// </summary>
    public bool IsBatchMode => _batchMode;

    /// <summary>
    /// Initializes a new instance of SqliteMemory.
    /// 初始化 SqliteMemory 的新实例。
    /// </summary>
    /// <param name="databasePath">File path to the SQLite database. / SQLite 数据库的文件路径。</param>
    public SqliteMemory(string databasePath)
    {
        var connectionString = $"Data Source={databasePath}";
        _dbContext = new MemoryDbContext(connectionString);
        _dbContext.Database.EnsureCreated();
        
        // Load existing messages from database into cache
        // 从数据库加载现有消息到缓存
        var entities = _dbContext.Messages.OrderBy(m => m.Timestamp).ToList();
        foreach (var entity in entities)
        {
            _cache.Add(EntityToMessage(entity));
        }
    }

    /// <summary>
    /// Begin batch mode - defer persistence until EndBatch().
    /// 开始批量模式 - 延迟持久化直到 EndBatch()。
    /// </summary>
    public void BeginBatch()
    {
        _batchMode = true;
    }

    /// <summary>
    /// End batch mode - persist all pending changes.
    /// 结束批量模式 - 持久化所有待处理的更改。
    /// </summary>
    public void EndBatch()
    {
        _batchMode = false;
        _dbContext.SaveChanges();
    }

    /// <summary>
    /// End batch mode - persist all pending changes (async).
    /// 结束批量模式 - 持久化所有待处理的更改（异步）。
    /// </summary>
    public async Task EndBatchAsync()
    {
        _batchMode = false;
        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Add message to memory and persist to database.
    /// 添加消息到内存并持久化到数据库。
    /// </summary>
    /// <param name="message">The message to add. / 要添加的消息。</param>
    public void Add(Msg message)
    {
        lock (_lock)
        {
            _cache.Add(message);
            
            var entity = MessageToEntity(message);
            _dbContext.Messages.Add(entity);
            
            // Persist immediately unless in batch mode
            // 除非在批量模式下，否则立即持久化
            if (!_batchMode)
            {
                _dbContext.SaveChanges();
            }
        }
    }

    /// <inheritdoc />
    public List<Msg> GetAll()
    {
        return _cache.GetAll();
    }

    /// <inheritdoc />
    public List<Msg> GetRecent(int count)
    {
        return _cache.GetRecent(count);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _cache.Clear();
        _dbContext.Messages.RemoveRange(_dbContext.Messages);
        _dbContext.SaveChanges();
    }

    /// <inheritdoc />
    public int Count()
    {
        return _cache.Count();
    }

    /// <inheritdoc />
    public bool Delete(string messageId)
    {
        lock (_lock)
        {
            var removed = _cache.Delete(messageId);
            var entities = _dbContext.Messages.Where(m => m.MessageId == messageId).ToList();
            if (entities.Count > 0)
            {
                _dbContext.Messages.RemoveRange(entities);
                _dbContext.SaveChanges();
            }
            return removed || entities.Count > 0;
        }
    }

    /// <inheritdoc />
    public async Task<List<Msg>> SearchAsync(string query, int limit = 10)
    {
        var entities = await _dbContext.Messages
            .Where(m => EF.Functions.Like(m.Content ?? "", $"%{query}%"))
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .ToListAsync();
        
        return entities.Select(EntityToMessage).ToList();
    }

    /// <inheritdoc />
    public Task SaveAsync()
    {
        return _dbContext.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task LoadAsync()
    {
        var entities = await _dbContext.Messages.ToListAsync();
        _cache.Clear();
        foreach (var entity in entities)
        {
            _cache.Add(EntityToMessage(entity));
        }
    }

    /// <summary>
    /// Converts a Msg to a database entity.
    /// 将 Msg 转换为数据库实体。
    /// </summary>
    /// <param name="message">The message to convert. / 要转换的消息。</param>
    /// <returns>The database entity. / 数据库实体。</returns>
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

    /// <summary>
    /// Converts a database entity back to a Msg.
    /// 将数据库实体转换回 Msg。
    /// </summary>
    /// <param name="entity">The database entity. / 数据库实体。</param>
    /// <returns>The reconstructed message. / 重建的消息。</returns>
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

    /// <summary>
    /// Disposes the database context synchronously.
    /// 同步释放数据库上下文。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes managed resources.
    /// 释放托管资源。
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources. / 是否释放托管资源。</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Flush pending changes if in batch mode
            // 如果在批量模式下，刷新待处理的更改
            if (_batchMode)
            {
                try { _dbContext.SaveChanges(); } catch { }
            }
            _dbContext?.Dispose();
        }

        _disposed = true;
    }

    /// <summary>
    /// Disposes the database context asynchronously.
    /// 异步释放数据库上下文。
    /// </summary>
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
