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
using AgentScope.Core.Formatter;
using AgentScope.Core.Formatter.Anthropic;
using Dto = AgentScope.Core.Formatter.Anthropic.Dto;
using AgentScope.Core.Message;
using Xunit;

// Alias for DTO types
using AnthropicRole = AgentScope.Core.Formatter.Anthropic.Dto.AnthropicRole;
using AnthropicResponse = AgentScope.Core.Formatter.Anthropic.Dto.AnthropicResponse;
using AnthropicRequest = AgentScope.Core.Formatter.Anthropic.Dto.AnthropicRequest;
using AnthropicContentBlock = AgentScope.Core.Formatter.Anthropic.Dto.AnthropicContentBlock;
using AnthropicUsage = AgentScope.Core.Formatter.Anthropic.Dto.AnthropicUsage;

namespace AgentScope.Core.Tests.Formatter.Anthropic;

/// <summary>
/// Tests for Anthropic Formatter
/// Anthropic 格式化器测试
/// </summary>
public class AnthropicFormatterTests
{
    #region Format Tests

    [Fact]
    public void Format_SingleUserMessage_CreatesValidRequest()
    {
        // Arrange
        var formatter = new AnthropicChatFormatter("claude-3-5-sonnet-20241022");
        var messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Hello, Claude!").Build()
        };

        // Act
        var request = formatter.Format(messages);

        // Assert
        Assert.NotNull(request);
        Assert.Equal("claude-3-5-sonnet-20241022", request.Model);
        Assert.Single(request.Messages);
        Assert.Equal(AnthropicRole.User, request.Messages[0].Role);
        Assert.Equal(4096, request.MaxTokens); // Default
    }

    [Fact]
    public void Format_WithSystemMessage_ExtractsToSystemParameter()
    {
        // Arrange
        var formatter = new AnthropicChatFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder().Role("system").TextContent("You are a helpful assistant.").Build(),
            Msg.Builder().Role("user").TextContent("Hello!").Build()
        };

        // Act
        var request = formatter.Format(messages);

        // Assert
        Assert.NotNull(request);
        Assert.NotNull(request.System);
        Assert.Single(request.System);
        Assert.Equal("You are a helpful assistant.", request.System[0].Text);
        Assert.Single(request.Messages); // System message removed from messages list
    }

    [Fact]
    public void Format_WithOptions_AppliesOptions()
    {
        // Arrange
        var formatter = new AnthropicChatFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Hello!").Build()
        };
        var options = new GenerateOptions
        {
            Temperature = 0.7,
            MaxTokens = 1000,
            TopP = 0.9,
            TopK = 50
        };

        // Act
        var request = formatter.Format(messages, options);

        // Assert
        Assert.Equal(0.7, request.Temperature);
        Assert.Equal(1000, request.MaxTokens);
        Assert.Equal(0.9, request.TopP);
        Assert.Equal(50, request.TopK);
    }

    [Fact]
    public void Format_WithTools_AddsToolsToRequest()
    {
        // Arrange
        var formatter = new AnthropicChatFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Calculate 1 + 1").Build()
        };
        var tools = new List<ToolSchema>
        {
            new()
            {
                Name = "calculator",
                Description = "A simple calculator",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>(),
                    ["required"] = new List<string>()
                }
            }
        };
        var options = new GenerateOptions
        {
            AdditionalBodyParams = new Dictionary<string, object> { ["tools"] = tools }
        };

        // Act
        var request = formatter.Format(messages, options);

        // Assert
        Assert.NotNull(request.Tools);
        Assert.Single(request.Tools);
        Assert.Equal("calculator", request.Tools![0].Name);
    }

    #endregion

    #region Parse Tests

    [Fact]
    public void Parse_TextResponse_ReturnsChatResponse()
    {
        // Arrange
        var formatter = new AnthropicChatFormatter();
        var response = new AnthropicResponse
        {
            Id = "msg_123",
            Type = "message",
            Role = AnthropicRole.Assistant,
            Model = "claude-3-5-sonnet-20241022",
            Content = new List<AnthropicContentBlock>
            {
                new Dto.TextBlock { Text = "Hello! How can I help you today?" }
            },
            Usage = new AnthropicUsage
            {
                InputTokens = 10,
                OutputTokens = 20
            }
        };
        var startTime = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = formatter.Parse(response, startTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("msg_123", result.Id);
        Assert.Equal("Hello! How can I help you today?", result.Content);
        Assert.Equal("claude-3-5-sonnet-20241022", result.Model);
        Assert.NotNull(result.Usage);
        Assert.Equal(10, result.Usage!.InputTokens);
        Assert.Equal(20, result.Usage.OutputTokens);
    }

    [Fact]
    public void Parse_ToolUseResponse_ReturnsToolCalls()
    {
        // Arrange
        var formatter = new AnthropicChatFormatter();
        var response = new AnthropicResponse
        {
            Id = "msg_456",
            Type = "message",
            Role = AnthropicRole.Assistant,
            Model = "claude-3-5-sonnet-20241022",
            Content = new List<AnthropicContentBlock>
            {
                new Dto.ToolUseBlock
                {
                    Id = "toolu_123",
                    Name = "calculator",
                    Input = new Dictionary<string, object> { ["expression"] = "1 + 1" }
                }
            },
            StopReason = "tool_use",
            Usage = new AnthropicUsage
            {
                InputTokens = 15,
                OutputTokens = 25
            }
        };
        var startTime = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = formatter.Parse(response, startTime);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ToolCalls);
        Assert.Single(result.ToolCalls);
        Assert.Equal("toolu_123", result.ToolCalls![0].Id);
        Assert.Equal("calculator", result.ToolCalls[0].Name);
    }

    [Fact]
    public void Parse_ThinkingBlock_ReturnsThinkingContent()
    {
        // Arrange
        var formatter = new AnthropicChatFormatter();
        var response = new AnthropicResponse
        {
            Id = "msg_789",
            Type = "message",
            Role = AnthropicRole.Assistant,
            Model = "claude-3-opus-20240229",
            Content = new List<AnthropicContentBlock>
            {
                new Dto.ThinkingBlock { Thinking = "Let me think about this..." },
                new Dto.TextBlock { Text = "The answer is 42." }
            },
            Usage = new AnthropicUsage
            {
                InputTokens = 20,
                OutputTokens = 30
            }
        };
        var startTime = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = formatter.Parse(response, startTime);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.True(result.Metadata!.ContainsKey("thinking"));
        Assert.Equal("Let me think about this...", result.Metadata["thinking"]);
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void Interface_Format_ReturnsListOfRequests()
    {
        // Arrange
        IFormatter<AnthropicRequest, AnthropicResponse, GenerateOptions> formatter = 
            new AnthropicChatFormatter("claude-3-5-sonnet-20241022");
        var messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Hello!").Build()
        };

        // Act
        var result = formatter.Format(messages);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("claude-3-5-sonnet-20241022", result[0].Model);
    }

    [Fact]
    public void Interface_ParseResponse_ReturnsModelResponse()
    {
        // Arrange
        IFormatter<AnthropicRequest, AnthropicResponse, GenerateOptions> formatter = 
            new AnthropicChatFormatter();
        var response = new AnthropicResponse
        {
            Id = "msg_123",
            Type = "message",
            Role = AnthropicRole.Assistant,
            Model = "claude-3-5-sonnet-20241022",
            Content = new List<AnthropicContentBlock> { new Dto.TextBlock { Text = "Hi!" } },
            Usage = new AnthropicUsage { InputTokens = 5, OutputTokens = 10 }
        };
        var startTime = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = formatter.ParseResponse(response, startTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Hi!", result.Text);
    }

    #endregion

    #region MessageConverter Tests

    [Fact]
    public void MessageConverter_UserMessage_ReturnsUserRole()
    {
        // Arrange
        var messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Hello!").Build()
        };

        // Act
        var result = AnthropicMessageConverter.Convert(messages);

        // Assert
        Assert.Single(result);
        Assert.Equal(AnthropicRole.User, result[0].Role);
    }

    [Fact]
    public void MessageConverter_AssistantMessage_ReturnsAssistantRole()
    {
        // Arrange
        var messages = new List<Msg>
        {
            Msg.Builder().Role("assistant").TextContent("Hello there!").Build()
        };

        // Act
        var result = AnthropicMessageConverter.Convert(messages);

        // Assert
        Assert.Single(result);
        Assert.Equal(AnthropicRole.Assistant, result[0].Role);
    }

    [Fact]
    public void MessageConverter_ToolResultMessage_ReturnsUserRoleWithToolResult()
    {
        // Arrange
        var messages = new List<Msg>
        {
            Msg.Builder()
                .Role("tool")
                .TextContent("42")
                .AddMetadata("tool_call_id", "toolu_123")
                .Build()
        };

        // Act
        var result = AnthropicMessageConverter.Convert(messages);

        // Assert
        Assert.Single(result);
        Assert.Equal(AnthropicRole.User, result[0].Role);
        Assert.Single(result[0].Content);
        Assert.IsType<Dto.ToolResultBlock>(result[0].Content[0]);
    }

    [Fact]
    public void MessageConverter_ExtractSystemMessage_ReturnsSystemMessages()
    {
        // Arrange
        var messages = new List<Msg>
        {
            Msg.Builder().Role("system").TextContent("You are helpful.").Build(),
            Msg.Builder().Role("user").TextContent("Hello!").Build()
        };

        // Act
        var systemMessages = AnthropicMessageConverter.ExtractSystemMessage(messages);

        // Assert
        Assert.NotNull(systemMessages);
        Assert.Single(systemMessages);
        Assert.Equal("You are helpful.", systemMessages![0].Text);
    }

    #endregion
}
