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

using Xunit;
using AgentScope.Core.Memory;
using AgentScope.Core.Message;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AgentScope.Core.Tests.Memory;

/// <summary>
/// Tests for <see cref="MemoryBase"/> covering add, get-recent, clear,
/// and thread-safe concurrent access.
/// <see cref="MemoryBase"/> 的添加、获取最近、清除以及线程安全并发访问测试。
/// </summary>
public class MemoryBaseTests
{
    /// <summary>
    /// Verifies that adding a message stores it in memory.
    /// 验证添加消息后该消息被存储在内存中。
    /// </summary>
    [Fact]
    public void MemoryBase_Add_ShouldStoreMessage()
    {
        // Arrange
        var memory = new MemoryBase();
        var msg = Msg.Builder().TextContent("Test").Build();

        // Act
        memory.Add(msg);

        // Assert
        Assert.Equal(1, memory.Count());
        Assert.Contains(msg, memory.GetAll());
    }

    /// <summary>
    /// Verifies that <see cref="MemoryBase.GetRecent"/> returns the last N messages.
    /// 验证 MemoryBase.GetRecent 返回最近的 N 条消息。
    /// </summary>
    [Fact]
    public void MemoryBase_GetRecent_ShouldReturnLastMessages()
    {
        // Arrange
        var memory = new MemoryBase();
        for (int i = 0; i < 5; i++)
        {
            memory.Add(Msg.Builder().TextContent($"Message {i}").Build());
        }

        // Act
        var recent = memory.GetRecent(3);

        // Assert
        Assert.Equal(3, recent.Count);
        Assert.Contains("Message 4", recent.Last().GetTextContent());
    }

    /// <summary>
    /// Verifies that <see cref="MemoryBase.Clear"/> removes all stored messages.
    /// 验证 MemoryBase.Clear 移除所有存储的消息。
    /// </summary>
    [Fact]
    public void MemoryBase_Clear_ShouldRemoveAllMessages()
    {
        // Arrange
        var memory = new MemoryBase();
        memory.Add(Msg.Builder().TextContent("Test1").Build());
        memory.Add(Msg.Builder().TextContent("Test2").Build());

        // Act
        memory.Clear();

        // Assert
        Assert.Equal(0, memory.Count());
    }

    /// <summary>
    /// Verifies that <see cref="MemoryBase"/> is thread-safe under concurrent access.
    /// 验证 MemoryBase 在并发访问下是线程安全的。
    /// </summary>
    [Fact]
    public async Task MemoryBase_ThreadSafe_ShouldHandleConcurrentAccess()
    {
        // Arrange
        var memory = new MemoryBase();
        var tasks = new Task[10];

        // Act
        for (int i = 0; i < 10; i++)
        {
            int index = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    memory.Add(Msg.Builder().TextContent($"Thread {index} Message {j}").Build());
                }
            });
        }
        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(100, memory.Count());
    }
}

/// <summary>
/// Tests for <see cref="SqliteMemory"/> covering persistence, search, get-recent,
/// clear, and metadata operations.
/// <see cref="SqliteMemory"/> 的持久化、搜索、获取最近、清除和元数据操作测试。
/// </summary>
public class SqliteMemoryTests : IDisposable
{
    private readonly string _testDbPath;

    /// <summary>
    /// Initializes a new test instance with a unique temporary database file.
    /// 使用唯一的临时数据库文件初始化新的测试实例。
    /// </summary>
    public SqliteMemoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_memory_{Guid.NewGuid()}.db");
    }

    /// <summary>
    /// Cleans up the temporary database file after each test.
    /// 每个测试后清理临时数据库文件。
    /// </summary>
    public void Dispose()
    {
        if (File.Exists(_testDbPath))
        {
            try
            {
                File.Delete(_testDbPath);
            }
            catch
            {
                // 忽略清理错误
            }
        }
    }

    /// <summary>
    /// Verifies that a message added to <see cref="SqliteMemory"/> is persisted.
    /// 验证添加到 SqliteMemory 的消息已被持久化。
    /// </summary>
    [Fact]
    public void SqliteMemory_Add_ShouldPersistMessage()
    {
        // Arrange
        using var memory = new SqliteMemory(_testDbPath);
        var msg = Msg.Builder()
            .Name("Agent")
            .Role("assistant")
            .TextContent("Test message")
            .Build();

        // Act
        memory.Add(msg);

        // Assert
        Assert.Equal(1, memory.Count());
        var retrieved = memory.GetAll().First();
        Assert.Equal(msg.Id, retrieved.Id);
        Assert.Equal("Test message", retrieved.GetTextContent());
    }

    /// <summary>
    /// Verifies that messages survive a dispose-and-recreate cycle (persistence).
    /// 验证消息在释放和重新创建周期后仍然存在（持久化）。
    /// </summary>
    [Fact]
    public void SqliteMemory_Persistence_ShouldLoadAfterRestart()
    {
        // Arrange
        var msg = Msg.Builder().TextContent("Persistent message").Build();
        
        // Act - Add and dispose
        using (var memory = new SqliteMemory(_testDbPath))
        {
            memory.Add(msg);
        }

        // Assert - Load in new instance
        using (var memory2 = new SqliteMemory(_testDbPath))
        {
            Assert.Equal(1, memory2.Count());
            var retrieved = memory2.GetAll().First();
            Assert.Equal("Persistent message", retrieved.GetTextContent());
        }
    }

    /// <summary>
    /// Verifies that <see cref="SqliteMemory.SearchAsync"/> finds messages matching the query.
    /// 验证 SqliteMemory.SearchAsync 找到与查询匹配的消息。
    /// </summary>
    [Fact]
    public async Task SqliteMemory_SearchAsync_ShouldFindMatchingMessages()
    {
        // Arrange
        using var memory = new SqliteMemory(_testDbPath);
        memory.Add(Msg.Builder().TextContent("Hello world").Build());
        memory.Add(Msg.Builder().TextContent("Goodbye world").Build());
        memory.Add(Msg.Builder().TextContent("Random message").Build());

        // Act
        var results = await memory.SearchAsync("world");

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, msg => Assert.Contains("world", msg.GetTextContent()));
    }

    /// <summary>
    /// Verifies that <see cref="SqliteMemory.GetRecent"/> returns the last N persisted messages.
    /// 验证 SqliteMemory.GetRecent 返回最近的 N 条持久化消息。
    /// </summary>
    [Fact]
    public void SqliteMemory_GetRecent_ShouldReturnLastMessages()
    {
        // Arrange
        using var memory = new SqliteMemory(_testDbPath);
        for (int i = 0; i < 5; i++)
        {
            memory.Add(Msg.Builder().TextContent($"Message {i}").Build());
        }

        // Act
        var recent = memory.GetRecent(3);

        // Assert
        Assert.Equal(3, recent.Count);
        Assert.Contains("Message 4", recent.Last().GetTextContent());
    }

    /// <summary>
    /// Verifies that <see cref="SqliteMemory.Clear"/> removes all persisted messages.
    /// 验证 SqliteMemory.Clear 移除所有持久化消息。
    /// </summary>
    [Fact]
    public void SqliteMemory_Clear_ShouldRemoveAllMessages()
    {
        // Arrange
        using var memory = new SqliteMemory(_testDbPath);
        memory.Add(Msg.Builder().TextContent("Test1").Build());
        memory.Add(Msg.Builder().TextContent("Test2").Build());

        // Act
        memory.Clear();

        // Assert
        Assert.Equal(0, memory.Count());
    }

    /// <summary>
    /// Verifies that metadata associated with a message is persisted by <see cref="SqliteMemory"/>.
    /// 验证与消息关联的元数据被 SqliteMemory 持久化。
    /// </summary>
    [Fact]
    public void SqliteMemory_WithMetadata_ShouldPersistMetadata()
    {
        // Arrange
        using var memory = new SqliteMemory(_testDbPath);
        var msg = Msg.Builder()
            .TextContent("Test")
            .AddMetadata("key1", "value1")
            .AddMetadata("key2", 42)
            .Build();

        // Act
        memory.Add(msg);
        var retrieved = memory.GetAll().First();

        // Assert
        Assert.NotNull(retrieved.Metadata);
        Assert.Equal("value1", retrieved.Metadata["key1"].ToString());
    }
}
