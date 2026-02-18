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
using AgentScope.Core.Formatter.DashScope;
using Dto = AgentScope.Core.Formatter.DashScope.Dto;
using AgentScope.Core.Message;
using Xunit;

// Alias for GenerateOptions to avoid ambiguity
using DashScopeGenerateOptions = AgentScope.Core.Formatter.DashScope.GenerateOptions;

namespace AgentScope.Core.Tests.Formatter.DashScope;

/// <summary>
/// Tests for DashScope Formatter
/// DashScope 格式化器测试
/// </summary>
public class DashScopeFormatterTests
{
    #region Format Tests

    [Fact]
    public void Format_SingleUserMessage_CreatesValidRequest()
    {
        // Arrange
        var formatter = new DashScopeChatFormatter("qwen-plus");
        var messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Hello, Qwen!").Build()
        };

        // Act
        var request = formatter.BuildRequest("qwen-plus", 
            DashScopeMessageConverter.Convert(messages), false);

        // Assert
        Assert.NotNull(request);
        Assert.Equal("qwen-plus", request.Model);
        Assert.Single(request.Input.Messages);
        Assert.Equal("user", request.Input.Messages[0].Role);
        Assert.Equal("Hello, Qwen!", request.Input.Messages[0].Content);
    }

    [Fact]
    public void Format_WithSystemMessage_IncludesSystemMessage()
    {
        // Arrange
        var formatter = new DashScopeChatFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder().Role("system").TextContent("You are a helpful assistant.").Build(),
            Msg.Builder().Role("user").TextContent("Hello!").Build()
        };

        // Act
        var request = formatter.BuildRequest("qwen-plus",
            DashScopeMessageConverter.Convert(messages), false);

        // Assert
        Assert.NotNull(request);
        Assert.Equal(2, request.Input.Messages.Count);
        Assert.Equal("system", request.Input.Messages[0].Role);
        Assert.Equal("user", request.Input.Messages[1].Role);
    }

    [Fact]
    public void Format_WithOptions_AppliesOptions()
    {
        // Arrange
        var formatter = new DashScopeChatFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Hello!").Build()
        };
        var dsMessages = DashScopeMessageConverter.Convert(messages);
        var options = new DashScopeGenerateOptions
        {
            Temperature = 0.7,
            MaxTokens = 1000,
            TopP = 0.9
        };

        // Act
        var request = formatter.BuildRequest("qwen-plus", dsMessages, false, 
            options, null, null, null);

        // Assert
        Assert.NotNull(request.Parameters);
        Assert.Equal(0.7, request.Parameters!.Temperature);
        Assert.Equal(1000, request.Parameters.MaxTokens);
        Assert.Equal(0.9, request.Parameters.TopP);
    }

    [Fact]
    public void Format_WithTools_AddsToolsToRequest()
    {
        // Arrange
        var formatter = new DashScopeChatFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder().Role("user").TextContent("Calculate 1 + 1").Build()
        };
        var dsMessages = DashScopeMessageConverter.Convert(messages);
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

        // Act
        var request = formatter.BuildRequest("qwen-plus", dsMessages, false,
            null, null, tools, null);

        // Assert
        Assert.NotNull(request.Parameters);
        Assert.NotNull(request.Parameters.Tools);
        Assert.Single(request.Parameters.Tools);
        Assert.Equal("calculator", request.Parameters.Tools[0].Function.Name);
    }

    [Fact]
    public void Format_WithMultimodal_UsesContentParts()
    {
        // Arrange
        var formatter = new DashScopeChatFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder()
                .Role("user")
                .TextContent("What's in this image?")
                .AddMetadata("image_urls", new List<string> { "https://example.com/image.jpg" })
                .Build()
        };

        // Act
        var dsMessages = DashScopeMessageConverter.Convert(messages, useMultimodalFormat: true);

        // Assert
        Assert.Single(dsMessages);
        Assert.True(dsMessages[0].IsMultimodal);
        var contentList = dsMessages[0].ContentAsList;
        Assert.NotNull(contentList);
        Assert.Equal(2, contentList.Count);
        Assert.Equal("What's in this image?", contentList[0].Text);
        Assert.Equal("https://example.com/image.jpg", contentList[1].Image);
    }

    #endregion

    #region Parse Tests

    [Fact]
    public void Parse_TextResponse_ReturnsChatResponse()
    {
        // Arrange
        var response = new Dto.DashScopeResponse
        {
            RequestId = "req_123",
            Output = new Dto.DashScopeOutput
            {
                Choices = new List<Dto.DashScopeChoice>
                {
                    new()
                    {
                        Message = new Dto.DashScopeMessage
                        {
                            Role = "assistant",
                            Content = "Hello! How can I help you today?"
                        },
                        FinishReason = "stop"
                    }
                }
            },
            Usage = new Dto.DashScopeUsage
            {
                InputTokens = 10,
                OutputTokens = 20
            }
        };
        var startTime = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = DashScopeResponseParser.ParseResponse(response, startTime);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("req_123", result.Id);
        Assert.Equal("Hello! How can I help you today?", result.Content);
        Assert.Equal("stop", result.StopReason);
        Assert.NotNull(result.Usage);
        Assert.Equal(10, result.Usage!.InputTokens);
        Assert.Equal(20, result.Usage.OutputTokens);
    }

    [Fact]
    public void Parse_ToolUseResponse_ReturnsToolCalls()
    {
        // Arrange
        var response = new Dto.DashScopeResponse
        {
            RequestId = "req_456",
            Output = new Dto.DashScopeOutput
            {
                Choices = new List<Dto.DashScopeChoice>
                {
                    new()
                    {
                        Message = new Dto.DashScopeMessage
                        {
                            Role = "assistant",
                            Content = "",
                            ToolCalls = new List<Dto.DashScopeToolCall>
                            {
                                new()
                                {
                                    Id = "call_123",
                                    Function = new Dto.DashScopeFunction
                                    {
                                        Name = "calculator",
                                        Arguments = "{\"expression\": \"1 + 1\"}"
                                    }
                                }
                            }
                        },
                        FinishReason = "tool_calls"
                    }
                }
            },
            Usage = new Dto.DashScopeUsage
            {
                InputTokens = 15,
                OutputTokens = 25
            }
        };
        var startTime = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = DashScopeResponseParser.ParseResponse(response, startTime);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ToolCalls);
        Assert.Single(result.ToolCalls);
        Assert.Equal("call_123", result.ToolCalls![0].Id);
        Assert.Equal("calculator", result.ToolCalls[0].Name);
    }

    [Fact]
    public void Parse_ThinkingContent_ReturnsThinkingInMetadata()
    {
        // Arrange
        var response = new Dto.DashScopeResponse
        {
            RequestId = "req_789",
            Output = new Dto.DashScopeOutput
            {
                Choices = new List<Dto.DashScopeChoice>
                {
                    new()
                    {
                        Message = new Dto.DashScopeMessage
                        {
                            Role = "assistant",
                            Content = "The answer is 42.",
                            ReasoningContent = "Let me think about this..."
                        },
                        FinishReason = "stop"
                    }
                }
            },
            Usage = new Dto.DashScopeUsage
            {
                InputTokens = 20,
                OutputTokens = 30
            }
        };
        var startTime = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var result = DashScopeResponseParser.ParseResponse(response, startTime);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Metadata);
        Assert.True(result.Metadata!.ContainsKey("thinking"));
        Assert.Equal("Let me think about this...", result.Metadata["thinking"]);
    }

    [Fact]
    public void Parse_ErrorResponse_IsErrorTrue()
    {
        // Arrange
        var response = new Dto.DashScopeResponse
        {
            RequestId = "req_error",
            Code = "InvalidParameter",
            Message = "Invalid API key"
        };

        // Act & Assert
        Assert.True(response.IsError);
        Assert.Equal("InvalidParameter", response.Code);
        Assert.Equal("Invalid API key", response.Message);
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
        var result = DashScopeMessageConverter.Convert(messages);

        // Assert
        Assert.Single(result);
        Assert.Equal("user", result[0].Role);
        Assert.Equal("Hello!", result[0].Content);
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
        var result = DashScopeMessageConverter.Convert(messages);

        // Assert
        Assert.Single(result);
        Assert.Equal("assistant", result[0].Role);
    }

    [Fact]
    public void MessageConverter_ToolResultMessage_ReturnsToolRole()
    {
        // Arrange
        var messages = new List<Msg>
        {
            Msg.Builder()
                .Role("tool")
                .TextContent("42")
                .AddMetadata("tool_call_id", "call_123")
                .AddMetadata("tool_name", "calculator")
                .Build()
        };

        // Act
        var result = DashScopeMessageConverter.Convert(messages);

        // Assert
        Assert.Single(result);
        Assert.Equal("tool", result[0].Role);
        Assert.Equal("call_123", result[0].ToolCallId);
        Assert.Equal("calculator", result[0].Name);
    }

    #endregion

}
