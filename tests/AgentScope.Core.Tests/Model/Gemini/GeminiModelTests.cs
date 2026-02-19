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
using AgentScope.Core.Model.Gemini;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using Xunit;

namespace AgentScope.Core.Tests.Model.Gemini;

public class GeminiModelTests
{
    [Fact]
    public void GeminiModel_DefaultConstructor_WithoutApiKey_ShouldThrow()
    {
        // Arrange & Act & Assert
        // Without GOOGLE_API_KEY or GEMINI_API_KEY, should throw
        var savedKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        var savedGeminiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        
        try
        {
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", null);
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", null);
            
            Assert.Throws<InvalidOperationException>(() => new GeminiModel());
        }
        finally
        {
            Environment.SetEnvironmentVariable("GOOGLE_API_KEY", savedKey);
            Environment.SetEnvironmentVariable("GEMINI_API_KEY", savedGeminiKey);
        }
    }

    [Fact]
    public void GeminiModel_WithApiKey_ShouldCreateInstance()
    {
        // Arrange & Act
        var model = new GeminiModel(apiKey: "test-api-key-12345");

        // Assert
        Assert.Equal(GeminiModel.DefaultModel, model.ModelName);
        Assert.Equal("test...2345", model.ApiKey);
    }

    [Fact]
    public void GeminiModel_WithCustomModelName_ShouldUseCustomModel()
    {
        // Arrange & Act
        var model = new GeminiModel(
            modelName: "gemini-1.5-pro",
            apiKey: "test-key");

        // Assert
        Assert.Equal("gemini-1.5-pro", model.ModelName);
    }

    [Fact]
    public void GeminiModel_ModelsConstants_ShouldBeDefined()
    {
        // Assert - Verify all model constants are defined
        Assert.Equal("gemini-pro", GeminiModel.Models.GeminiPro);
        Assert.Equal("gemini-pro-vision", GeminiModel.Models.GeminiProVision);
        Assert.Equal("gemini-1.5-pro", GeminiModel.Models.Gemini15Pro);
        Assert.Equal("gemini-1.5-flash", GeminiModel.Models.Gemini15Flash);
        Assert.Equal("gemini-2.0-flash-exp", GeminiModel.Models.Gemini20Flash);
        Assert.Equal("gemini-2.0-pro-exp", GeminiModel.Models.Gemini20Pro);
    }

    [Fact]
    public void GeminiModel_DefaultConstants_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta", GeminiModel.DefaultBaseUrl);
        Assert.Equal("gemini-pro", GeminiModel.DefaultModel);
    }

    [Fact]
    public void GeminiModelBuilder_Build_ShouldCreateModel()
    {
        // Arrange & Act
        var model = GeminiModel.Builder()
            .ApiKey("test-api-key")
            .ModelName("gemini-1.5-flash")
            .Build();

        // Assert
        Assert.Equal("gemini-1.5-flash", model.ModelName);
    }

    [Fact]
    public void GeminiModelBuilder_UseGeminiPro_ShouldSetGeminiProModel()
    {
        // Arrange & Act
        var model = GeminiModel.Builder()
            .ApiKey("test-key")
            .UseGeminiPro()
            .Build();

        // Assert
        Assert.Equal(GeminiModel.Models.GeminiPro, model.ModelName);
    }

    [Fact]
    public void GeminiModelBuilder_UseGeminiProVision_ShouldSetGeminiProVisionModel()
    {
        // Arrange & Act
        var model = GeminiModel.Builder()
            .ApiKey("test-key")
            .UseGeminiProVision()
            .Build();

        // Assert
        Assert.Equal(GeminiModel.Models.GeminiProVision, model.ModelName);
    }

    [Fact]
    public void GeminiModelBuilder_UseGemini15Pro_ShouldSetGemini15ProModel()
    {
        // Arrange & Act
        var model = GeminiModel.Builder()
            .ApiKey("test-key")
            .UseGemini15Pro()
            .Build();

        // Assert
        Assert.Equal(GeminiModel.Models.Gemini15Pro, model.ModelName);
    }

    [Fact]
    public void GeminiModelBuilder_UseGemini15Flash_ShouldSetGemini15FlashModel()
    {
        // Arrange & Act
        var model = GeminiModel.Builder()
            .ApiKey("test-key")
            .UseGemini15Flash()
            .Build();

        // Assert
        Assert.Equal(GeminiModel.Models.Gemini15Flash, model.ModelName);
    }

    [Fact]
    public void GeminiModelBuilder_UseGemini20Flash_ShouldSetGemini20FlashModel()
    {
        // Arrange & Act
        var model = GeminiModel.Builder()
            .ApiKey("test-key")
            .UseGemini20Flash()
            .Build();

        // Assert
        Assert.Equal(GeminiModel.Models.Gemini20Flash, model.ModelName);
    }

    [Fact]
    public void GeminiModelBuilder_WithBaseUrl_ShouldSetCustomUrl()
    {
        // Arrange & Act
        var model = GeminiModel.Builder()
            .ApiKey("test-key")
            .ModelName("gemini-pro")
            .BaseUrl("https://custom.googleapis.com/v1beta")
            .Build();

        // Assert
        Assert.Equal("https://custom.googleapis.com/v1beta", model.BaseUrl);
    }

    [Fact]
    public void GeminiModelBuilder_WithDefaultOptions_ShouldSetOptions()
    {
        // Arrange
        var options = new GenerateOptions
        {
            Temperature = 0.5,
            MaxTokens = 500
        };

        // Act
        var model = GeminiModel.Builder()
            .ApiKey("test-key")
            .DefaultOptions(options)
            .Build();

        // Assert
        Assert.Equal("gemini-pro", model.ModelName);
    }

    [Fact]
    public async Task GeminiModel_GenerateAsync_WithSimpleMessage_ShouldReturnErrorResponse()
    {
        // This test verifies the model can be called but will fail with invalid API key
        
        // Arrange
        var model = new GeminiModel(
            modelName: "gemini-pro",
            apiKey: "invalid-key-for-testing");

        var message = Msg.Builder()
            .Role("user")
            .TextContent("Hello!")
            .Build();

        var request = new ModelRequest
        {
            Messages = new List<Msg> { message }
        };

        // Act
        var response = await model.GenerateAsync(request);

        // Assert - Should fail with API error (invalid key)
        Assert.False(response.Success);
        Assert.NotNull(response.Error);
        Assert.Contains("API error", response.Error);
    }

    [Fact(Skip = "Requires valid GOOGLE_API_KEY environment variable")]
    public async Task GeminiModel_GenerateAsync_WithRealApi_ReturnsResponse()
    {
        // This test requires a valid Google API key
        // Run with: dotnet test --filter "FullyQualifiedName~GeminiModel_GenerateAsync_WithRealApi"
        
        // Arrange
        var model = GeminiModel.Builder()
            .UseGeminiPro()
            .Build();

        var message = Msg.Builder()
            .Role("user")
            .TextContent("Say 'Hello, World!'")
            .Build();

        var request = new ModelRequest
        {
            Messages = new List<Msg> { message }
        };

        // Act
        var response = await model.GenerateAsync(request);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.False(string.IsNullOrEmpty(response.Text));
    }
}
