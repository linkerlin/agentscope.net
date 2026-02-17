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
using System.Threading.Tasks;
using AgentScope.Core.Message;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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
    public DbSet<MessageEntity> Messages { get; set; }

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
/// SQLite-based persistent memory implementation
/// </summary>
public class SqliteMemory : IPersistentMemory
{
    private readonly MemoryDbContext _dbContext;
    private readonly MemoryBase _cache = new();

    public SqliteMemory(string databasePath)
    {
        var connectionString = $"Data Source={databasePath}";
        _dbContext = new MemoryDbContext(connectionString);
        _dbContext.Database.EnsureCreated();
    }

    public void Add(Msg message)
    {
        _cache.Add(message);
        
        var entity = MessageToEntity(message);
        _dbContext.Messages.Add(entity);
        _dbContext.SaveChanges();
    }

    public List<Msg> GetAll()
    {
        var entities = _dbContext.Messages.OrderBy(m => m.Timestamp).ToList();
        return entities.Select(EntityToMessage).ToList();
    }

    public List<Msg> GetRecent(int count)
    {
        var entities = _dbContext.Messages
            .OrderByDescending(m => m.Timestamp)
            .Take(count)
            .OrderBy(m => m.Timestamp)
            .ToList();
        return entities.Select(EntityToMessage).ToList();
    }

    public void Clear()
    {
        _cache.Clear();
        _dbContext.Messages.RemoveRange(_dbContext.Messages);
        _dbContext.SaveChanges();
    }

    public int Count()
    {
        return _dbContext.Messages.Count();
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
            Metadata = message.Metadata != null ? JsonConvert.SerializeObject(message.Metadata) : null,
            Url = message.Url != null ? JsonConvert.SerializeObject(message.Url) : null
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
                ? JsonConvert.DeserializeObject<Dictionary<string, object>>(entity.Metadata) 
                : null,
            Url = entity.Url != null 
                ? JsonConvert.DeserializeObject<List<string>>(entity.Url) 
                : null
        };
    }

    public void Dispose()
    {
        _dbContext?.Dispose();
    }
}
