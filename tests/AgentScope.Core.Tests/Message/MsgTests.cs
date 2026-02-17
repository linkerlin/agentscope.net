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
using AgentScope.Core.Message;
using System;
using System.Collections.Generic;

namespace AgentScope.Core.Tests.Message;

public class MsgTests
{
    [Fact]
    public void Msg_DefaultConstructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var msg = new Msg();

        // Assert
        Assert.NotNull(msg.Id);
        Assert.NotEmpty(msg.Id);
        Assert.Equal("user", msg.Role);
        Assert.True(msg.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public void Msg_Constructor_WithParameters_ShouldSetProperties()
    {
        // Arrange
        var name = "TestAgent";
        var content = "Hello World";
        var role = "assistant";

        // Act
        var msg = new Msg(name, content, role);

        // Assert
        Assert.Equal(name, msg.Name);
        Assert.Equal(content, msg.Content);
        Assert.Equal(role, msg.Role);
    }

    [Fact]
    public void Msg_GetTextContent_WithStringContent_ShouldReturnString()
    {
        // Arrange
        var msg = new Msg { Content = "Test content" };

        // Act
        var result = msg.GetTextContent();

        // Assert
        Assert.Equal("Test content", result);
    }

    [Fact]
    public void Msg_GetTextContent_WithDictionaryContent_ShouldReturnTextValue()
    {
        // Arrange
        var msg = new Msg
        {
            Content = new Dictionary<string, object>
            {
                ["text"] = "Dictionary text"
            }
        };

        // Act
        var result = msg.GetTextContent();

        // Assert
        Assert.Equal("Dictionary text", result);
    }

    [Fact]
    public void Msg_SetTextContent_ShouldUpdateContent()
    {
        // Arrange
        var msg = new Msg();
        var newContent = "New content";

        // Act
        msg.SetTextContent(newContent);

        // Assert
        Assert.Equal(newContent, msg.Content);
    }

    [Fact]
    public void MsgBuilder_Build_ShouldCreateMsg()
    {
        // Arrange & Act
        var msg = Msg.Builder()
            .Name("Agent")
            .Role("assistant")
            .TextContent("Hello")
            .Build();

        // Assert
        Assert.Equal("Agent", msg.Name);
        Assert.Equal("assistant", msg.Role);
        Assert.Equal("Hello", msg.Content);
    }

    [Fact]
    public void MsgBuilder_WithMetadata_ShouldSetMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["key1"] = "value1",
            ["key2"] = 42
        };

        // Act
        var msg = Msg.Builder()
            .Metadata(metadata)
            .Build();

        // Assert
        Assert.NotNull(msg.Metadata);
        Assert.Equal(2, msg.Metadata.Count);
        Assert.Equal("value1", msg.Metadata["key1"]);
        Assert.Equal(42, msg.Metadata["key2"]);
    }

    [Fact]
    public void MsgBuilder_AddMetadata_ShouldAddIndividualItem()
    {
        // Arrange & Act
        var msg = Msg.Builder()
            .AddMetadata("key1", "value1")
            .AddMetadata("key2", 42)
            .Build();

        // Assert
        Assert.NotNull(msg.Metadata);
        Assert.Equal(2, msg.Metadata.Count);
        Assert.Equal("value1", msg.Metadata["key1"]);
    }

    [Fact]
    public void MsgBuilder_WithUrl_ShouldSetUrls()
    {
        // Arrange
        var urls = new List<string> { "http://example.com", "http://test.com" };

        // Act
        var msg = Msg.Builder()
            .Url(urls)
            .Build();

        // Assert
        Assert.NotNull(msg.Url);
        Assert.Equal(2, msg.Url.Count);
        Assert.Contains("http://example.com", msg.Url);
    }

    [Fact]
    public void Msg_ToString_ShouldReturnValidJson()
    {
        // Arrange
        var msg = Msg.Builder()
            .Name("TestAgent")
            .Role("assistant")
            .TextContent("Test")
            .Build();

        // Act
        var json = msg.ToString();

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"name\"", json);
        Assert.Contains("TestAgent", json);
        Assert.Contains("\"role\"", json);
        Assert.Contains("assistant", json);
    }
}
