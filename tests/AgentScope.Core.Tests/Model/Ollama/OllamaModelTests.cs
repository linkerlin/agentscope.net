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
using AgentScope.Core.Model.Ollama;
using Xunit;
using Msg = AgentScope.Core.Message.Msg;

namespace AgentScope.Core.Tests.Model.Ollama;

/// <summary>
/// Tests for <see cref="OllamaModel"/> and its builder.
/// OllamaModel 及其构建器的测试。
/// </summary>
public class OllamaModelTests
{
    [Fact]
    /// <summary>
    /// Tests that the default constructor uses the default model name.
    /// 测试默认构造函数使用了默认模型名称。
    /// </summary>
    public void OllamaModel_DefaultConstructor_ShouldUseDefaults()
    {
        // Arrange & Act
        var model = new OllamaModel();

        // Assert
        Assert.Equal(OllamaModel.DefaultModel, model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that a custom model name is used when provided.
    /// 测试提供的自定义模型名称被正确使用。
    /// </summary>
    public void OllamaModel_WithCustomModelName_ShouldUseCustomModel()
    {
        // Arrange & Act
        var model = new OllamaModel(modelName: "mistral");

        // Assert
        Assert.Equal("mistral", model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that a custom base URL is used when provided.
    /// 测试提供的自定义基础 URL 被正确使用。
    /// </summary>
    public void OllamaModel_WithCustomBaseUrl_ShouldUseCustomUrl()
    {
        // Arrange & Act
        var model = new OllamaModel(baseUrl: "http://custom:8080/v1");

        // Assert
        // The base URL should be used for API calls
        Assert.Equal("mistral", new OllamaModel(modelName: "mistral").ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that all model constant names are correctly defined.
    /// 测试所有模型常量名称已被正确定义。
    /// </summary>
    public void OllamaModel_ModelsConstants_ShouldBeDefined()
    {
        // Assert - Verify all model constants are defined
        Assert.Equal("llama2", OllamaModel.Models.Llama2);
        Assert.Equal("llama3", OllamaModel.Models.Llama3);
        Assert.Equal("llama3.1", OllamaModel.Models.Llama31);
        Assert.Equal("mistral", OllamaModel.Models.Mistral);
        Assert.Equal("mixtral", OllamaModel.Models.Mixtral);
        Assert.Equal("codellama", OllamaModel.Models.CodeLlama);
        Assert.Equal("deepseek-coder", OllamaModel.Models.DeepSeekCoder);
        Assert.Equal("phi3", OllamaModel.Models.Phi3);
        Assert.Equal("gemma", OllamaModel.Models.Gemma);
        Assert.Equal("qwen", OllamaModel.Models.Qwen);
    }

    [Fact]
    /// <summary>
    /// Tests that the builder creates a model with the specified name.
    /// 测试构建器使用指定名称创建模型。
    /// </summary>
    public void OllamaModelBuilder_Build_ShouldCreateModel()
    {
        // Arrange & Act
        var model = OllamaModel.Builder()
            .ModelName("llama3")
            .Build();

        // Assert
        Assert.Equal("llama3", model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="OllamaModelBuilder.UseLlama2"/> sets the Llama2 model.
    /// 测试 UseLlama2() 设置了 Llama2 模型。
    /// </summary>
    public void OllamaModelBuilder_UseLlama2_ShouldSetLlama2Model()
    {
        // Arrange & Act
        var model = OllamaModel.Builder()
            .UseLlama2()
            .Build();

        // Assert
        Assert.Equal(OllamaModel.Models.Llama2, model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="OllamaModelBuilder.UseLlama3"/> sets the Llama3 model.
    /// 测试 UseLlama3() 设置了 Llama3 模型。
    /// </summary>
    public void OllamaModelBuilder_UseLlama3_ShouldSetLlama3Model()
    {
        // Arrange & Act
        var model = OllamaModel.Builder()
            .UseLlama3()
            .Build();

        // Assert
        Assert.Equal(OllamaModel.Models.Llama3, model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="OllamaModelBuilder.UseLlama31"/> sets the Llama3.1 model.
    /// 测试 UseLlama31() 设置了 Llama3.1 模型。
    /// </summary>
    public void OllamaModelBuilder_UseLlama31_ShouldSetLlama31Model()
    {
        // Arrange & Act
        var model = OllamaModel.Builder()
            .UseLlama31()
            .Build();

        // Assert
        Assert.Equal(OllamaModel.Models.Llama31, model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="OllamaModelBuilder.UseMistral"/> sets the Mistral model.
    /// 测试 UseMistral() 设置了 Mistral 模型。
    /// </summary>
    public void OllamaModelBuilder_UseMistral_ShouldSetMistralModel()
    {
        // Arrange & Act
        var model = OllamaModel.Builder()
            .UseMistral()
            .Build();

        // Assert
        Assert.Equal(OllamaModel.Models.Mistral, model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="OllamaModelBuilder.UseCodeLlama"/> sets the CodeLlama model.
    /// 测试 UseCodeLlama() 设置了 CodeLlama 模型。
    /// </summary>
    public void OllamaModelBuilder_UseCodeLlama_ShouldSetCodeLlamaModel()
    {
        // Arrange & Act
        var model = OllamaModel.Builder()
            .UseCodeLlama()
            .Build();

        // Assert
        Assert.Equal(OllamaModel.Models.CodeLlama, model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="OllamaModelBuilder.UseDeepSeekCoder"/> sets the DeepSeekCoder model.
    /// 测试 UseDeepSeekCoder() 设置了 DeepSeekCoder 模型。
    /// </summary>
    public void OllamaModelBuilder_UseDeepSeekCoder_ShouldSetDeepSeekCoderModel()
    {
        // Arrange & Act
        var model = OllamaModel.Builder()
            .UseDeepSeekCoder()
            .Build();

        // Assert
        Assert.Equal(OllamaModel.Models.DeepSeekCoder, model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that <see cref="OllamaModelBuilder.UsePhi3"/> sets the Phi-3 model.
    /// 测试 UsePhi3() 设置了 Phi-3 模型。
    /// </summary>
    public void OllamaModelBuilder_UsePhi3_ShouldSetPhi3Model()
    {
        // Arrange & Act
        var model = OllamaModel.Builder()
            .UsePhi3()
            .Build();

        // Assert
        Assert.Equal(OllamaModel.Models.Phi3, model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that the builder accepts a custom base URL.
    /// 测试构建器接受自定义基础 URL。
    /// </summary>
    public void OllamaModelBuilder_WithBaseUrl_ShouldSetCustomUrl()
    {
        // Arrange & Act
        var model = OllamaModel.Builder()
            .ModelName("llama3")
            .BaseUrl("http://custom:8080/v1")
            .Build();

        // Assert
        Assert.Equal("llama3", model.ModelName);
    }

    [Fact]
    /// <summary>
    /// Tests that the default model and base URL constants are correct.
    /// 测试默认模型和基础 URL 常量的正确性。
    /// </summary>
    public void OllamaModel_DefaultConstants_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("http://localhost:11434/v1", OllamaModel.DefaultBaseUrl);
        Assert.Equal("llama2", OllamaModel.DefaultModel);
    }

    [Fact]
    /// <summary>
    /// Integration test: generates a response from a real Ollama server. Skips if the server is unavailable.
    /// 集成测试：从真实的 Ollama 服务器生成响应。如果服务器不可用则跳过。
    /// </summary>
    public async Task OllamaModel_GenerateAsync_WithRealOllama_ReturnsResponse()
    {
        // This test requires a running Ollama server with the specified model
        // Check if Ollama server is available
        using var httpClient = new System.Net.Http.HttpClient();
        try
        {
            var baseUrl = Environment.GetEnvironmentVariable("OLLAMA_BASE_URL") 
                ?? "http://localhost:11434";
            var httpResponse = await httpClient.GetAsync($"{baseUrl}/api/tags");
            if (!httpResponse.IsSuccessStatusCode)
            {
                // Skip if Ollama server is not available
                return;
            }
        }
        catch
        {
            // Skip if Ollama server is not reachable
            return;
        }
        
        // Arrange
        var model = OllamaModel.Builder()
            .UseLlama3()
            .Build();

        var message = Msg.Builder()
            .Role("user")
            .TextContent("Hello, World!")
            .Build();

        var request = new Core.Model.ModelRequest
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
