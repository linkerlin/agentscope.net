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

public class MemoryBaseTests
{
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

public class SqliteMemoryTests : IDisposable
{
    private readonly string _testDbPath;

    public SqliteMemoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"test_memory_{Guid.NewGuid()}.db");
    }

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
                // Ignore cleanup errors
            }
        }
    }

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
