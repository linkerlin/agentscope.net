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
using AgentScope.Core.Formatter;
using AgentScope.Core.Formatter.Gemini;
using AgentScope.Core.Formatter.Gemini.Dto;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using Xunit;

namespace AgentScope.Core.Tests.Formatter.Gemini;

public class GeminiFormatterTests
{
    [Fact]
    public void GeminiFormatter_Format_SingleUserMessage_ShouldConvertCorrectly()
    {
        // Arrange
        var formatter = new GeminiFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder()
                .Role("user")
                .TextContent("Hello, Gemini!")
                .Build()
        };

        // Act
        var contents = formatter.Format(messages);

        // Assert
        Assert.Single(contents);
        Assert.Equal("user", contents[0].Role);
        Assert.Single(contents[0].Parts);
        Assert.Equal("Hello, Gemini!", contents[0].Parts[0].Text);
    }

    [Fact]
    public void GeminiFormatter_Format_AssistantMessage_ShouldUseModelRole()
    {
        // Arrange
        var formatter = new GeminiFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder()
                .Role("assistant")
                .TextContent("Hello!")
                .Build()
        };

        // Act
        var contents = formatter.Format(messages);

        // Assert
        Assert.Single(contents);
        Assert.Equal("model", contents[0].Role); // Gemini uses "model" instead of "assistant"
    }

    [Fact]
    public void GeminiFormatter_Format_MultipleMessages_ShouldConvertAll()
    {
        // Arrange
        var formatter = new GeminiFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder()
                .Role("user")
                .TextContent("Hello!")
                .Build(),
            Msg.Builder()
                .Role("assistant")
                .TextContent("Hi there!")
                .Build(),
            Msg.Builder()
                .Role("user")
                .TextContent("How are you?")
                .Build()
        };

        // Act
        var contents = formatter.Format(messages);

        // Assert
        Assert.Equal(3, contents.Count);
        Assert.Equal("user", contents[0].Role);
        Assert.Equal("model", contents[1].Role);
        Assert.Equal("user", contents[2].Role);
    }

    [Fact]
    public void GeminiFormatter_CreateRequest_WithSystemMessage_ShouldSetSystemInstruction()
    {
        // Arrange
        var formatter = new GeminiFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder()
                .Role("system")
                .TextContent("You are a helpful assistant.")
                .Build(),
            Msg.Builder()
                .Role("user")
                .TextContent("Hello!")
                .Build()
        };

        // Act
        var request = formatter.CreateRequest(messages);

        // Assert
        Assert.Single(request.Contents); // System message should be extracted
        Assert.NotNull(request.SystemInstruction);
        Assert.Equal("You are a helpful assistant.", request.SystemInstruction.Parts[0].Text);
    }

    [Fact]
    public void GeminiFormatter_CreateRequest_WithOptions_ShouldApplyOptions()
    {
        // Arrange
        var formatter = new GeminiFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder()
                .Role("user")
                .TextContent("Hello!")
                .Build()
        };

        var options = new GenerateOptions
        {
            Temperature = 0.7,
            MaxTokens = 1000,
            TopP = 0.9,
            TopK = 50
        };

        // Act
        var request = formatter.CreateRequest(messages, options);

        // Assert
        Assert.NotNull(request.GenerationConfig);
        Assert.Equal(0.7, request.GenerationConfig.Temperature);
        Assert.Equal(1000, request.GenerationConfig.MaxOutputTokens);
        Assert.Equal(0.9, request.GenerationConfig.TopP);
        Assert.Equal(50, request.GenerationConfig.TopK);
    }

    [Fact]
    public void GeminiFormatter_CreateRequest_WithTools_ShouldSetTools()
    {
        // Arrange
        var formatter = new GeminiFormatter();
        var messages = new List<Msg>
        {
            Msg.Builder()
                .Role("user")
                .TextContent("What's the weather?")
                .Build()
        };

        var tools = new List<ToolSchema>
        {
            new ToolSchema
            {
                Name = "get_weather",
                Description = "Get current weather",
                Parameters = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["location"] = new Dictionary<string, object>
                        {
                            ["type"] = "string",
                            ["description"] = "City name"
                        }
                    }
                }
            }
        };

        // Act
        var request = formatter.CreateRequest(messages, tools: tools);

        // Assert
        Assert.NotNull(request.Tools);
        Assert.Single(request.Tools);
        Assert.Single(request.Tools[0].FunctionDeclarations!);
        Assert.Equal("get_weather", request.Tools[0].FunctionDeclarations![0].Name);
    }

    [Fact]
    public void GeminiFormatter_Format_EmptyList_ShouldReturnEmpty()
    {
        // Arrange
        var formatter = new GeminiFormatter();
        var messages = new List<Msg>();

        // Act
        var contents = formatter.Format(messages);

        // Assert
        Assert.Empty(contents);
    }

    [Fact]
    public void GeminiFormatter_Format_NullList_ShouldReturnEmpty()
    {
        // Arrange
        var formatter = new GeminiFormatter();

        // Act
        var contents = formatter.Format(null!);

        // Assert
        Assert.Empty(contents);
    }
}

public class GeminiMessageConverterTests
{
    [Fact]
    public void ConvertToContent_UserMessage_ShouldConvertCorrectly()
    {
        // Arrange
        var msg = Msg.Builder()
            .Role("user")
            .TextContent("Hello!")
            .Build();

        // Act
        var content = GeminiMessageConverter.ConvertToContent(msg);

        // Assert
        Assert.Equal("user", content.Role);
        Assert.Single(content.Parts);
        Assert.Equal("Hello!", content.Parts[0].Text);
    }

    [Fact]
    public void ConvertToContent_AssistantMessage_ShouldUseModelRole()
    {
        // Arrange
        var msg = Msg.Builder()
            .Role("assistant")
            .TextContent("Hi!")
            .Build();

        // Act
        var content = GeminiMessageConverter.ConvertToContent(msg);

        // Assert
        Assert.Equal("model", content.Role);
    }

    [Fact]
    public void ConvertToContent_WithTextBlock_ShouldConvertText()
    {
        // Arrange
        var msg = Msg.Builder()
            .Role("user")
            .Content(new List<ContentBlock>
            {
                new TextBlock { Text = "Hello from text block!" }
            })
            .Build();

        // Act
        var content = GeminiMessageConverter.ConvertToContent(msg);

        // Assert
        Assert.Single(content.Parts);
        Assert.Equal("Hello from text block!", content.Parts[0].Text);
    }

    [Fact]
    public void ConvertToContent_WithToolUseBlock_ShouldConvertToFunctionCall()
    {
        // Arrange
        var msg = Msg.Builder()
            .Role("assistant")
            .Content(new List<ContentBlock>
            {
                new ToolUseBlock
                {
                    Id = "call_123",
                    Name = "get_weather",
                    Input = new Dictionary<string, object>
                    {
                        ["location"] = "Beijing"
                    }
                }
            })
            .Build();

        // Act
        var content = GeminiMessageConverter.ConvertToContent(msg);

        // Assert
        Assert.Single(content.Parts);
        Assert.NotNull(content.Parts[0].FunctionCall);
        Assert.Equal("get_weather", content.Parts[0].FunctionCall.Name);
    }

    [Fact]
    public void ConvertToContent_WithToolResultBlock_ShouldConvertToFunctionResponse()
    {
        // Arrange
        var msg = Msg.Builder()
            .Role("user")
            .Content(new List<ContentBlock>
            {
                new ToolResultBlock
                {
                    Id = "get_weather",
                    Output = "Sunny, 25°C"
                }
            })
            .Build();

        // Act
        var content = GeminiMessageConverter.ConvertToContent(msg);

        // Assert
        Assert.Single(content.Parts);
        Assert.NotNull(content.Parts[0].FunctionResponse);
        Assert.Equal("get_weather", content.Parts[0].FunctionResponse.Name);
    }

    [Fact]
    public void ConvertToContent_NullMessage_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => GeminiMessageConverter.ConvertToContent(null!));
    }
}

public class GeminiResponseParserTests
{
    [Fact]
    public void ParseResponse_ValidResponse_ShouldReturnModelResponse()
    {
        // Arrange
        var response = new GeminiResponse
        {
            Candidates = new List<GeminiCandidate>
            {
                new GeminiCandidate
                {
                    Content = new GeminiContent
                    {
                        Role = "model",
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = "Hello, I'm Gemini!" }
                        }
                    },
                    FinishReason = "STOP"
                }
            },
            ModelVersion = "gemini-pro"
        };

        // Act
        var result = GeminiResponseParser.ParseResponse(response, DateTime.UtcNow);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Hello, I'm Gemini!", result.Text);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ParseResponse_WithFunctionCall_ShouldReturnChatResponse()
    {
        // Arrange
        var response = new GeminiResponse
        {
            Candidates = new List<GeminiCandidate>
            {
                new GeminiCandidate
                {
                    Content = new GeminiContent
                    {
                        Role = "model",
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart
                            {
                                FunctionCall = new GeminiFunctionCall
                                {
                                    Name = "get_weather",
                                    Args = new Dictionary<string, object>
                                    {
                                        ["location"] = "Beijing"
                                    }
                                }
                            }
                        }
                    },
                    FinishReason = "STOP"
                }
            }
        };

        // Act
        var result = GeminiResponseParser.ParseResponse(response, DateTime.UtcNow);

        // Assert
        Assert.True(result.Success);
        Assert.IsType<ChatResponse>(result);
        var chatResponse = (ChatResponse)result;
        Assert.NotNull(chatResponse.ToolCalls);
        Assert.Single(chatResponse.ToolCalls);
        Assert.Equal("get_weather", chatResponse.ToolCalls[0].Name);
    }

    [Fact]
    public void ParseResponse_NoCandidates_ShouldReturnErrorResponse()
    {
        // Arrange
        var response = new GeminiResponse
        {
            Candidates = null
        };

        // Act
        var result = GeminiResponseParser.ParseResponse(response, DateTime.UtcNow);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No candidates in response", result.Error);
    }

    [Fact]
    public void ParseResponse_EmptyCandidates_ShouldReturnErrorResponse()
    {
        // Arrange
        var response = new GeminiResponse
        {
            Candidates = new List<GeminiCandidate>()
        };

        // Act
        var result = GeminiResponseParser.ParseResponse(response, DateTime.UtcNow);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("No candidates in response", result.Error);
    }

    [Fact]
    public void ParseResponse_NoContent_ShouldReturnErrorResponse()
    {
        // Arrange
        var response = new GeminiResponse
        {
            Candidates = new List<GeminiCandidate>
            {
                new GeminiCandidate
                {
                    Content = null,
                    FinishReason = "SAFETY"
                }
            }
        };

        // Act
        var result = GeminiResponseParser.ParseResponse(response, DateTime.UtcNow);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("No content", result.Error);
    }

    [Fact]
    public void ParseResponse_NonStopFinishReason_ShouldSetError()
    {
        // Arrange
        var response = new GeminiResponse
        {
            Candidates = new List<GeminiCandidate>
            {
                new GeminiCandidate
                {
                    Content = new GeminiContent
                    {
                        Role = "model",
                        Parts = new List<GeminiPart>
                        {
                            new GeminiPart { Text = "I can't respond to that." }
                        }
                    },
                    FinishReason = "SAFETY"
                }
            }
        };

        // Act
        var result = GeminiResponseParser.ParseResponse(response, DateTime.UtcNow);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("SAFETY", result.Error);
    }
}
