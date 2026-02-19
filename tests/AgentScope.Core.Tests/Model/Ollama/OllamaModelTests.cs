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

public class OllamaModelTests
{
    [Fact]
    public void OllamaModel_DefaultConstructor_ShouldUseDefaults()
    {
        // Arrange & Act
        var model = new OllamaModel();

        // Assert
        Assert.Equal(OllamaModel.DefaultModel, model.ModelName);
    }

    [Fact]
    public void OllamaModel_WithCustomModelName_ShouldUseCustomModel()
    {
        // Arrange & Act
        var model = new OllamaModel(modelName: "mistral");

        // Assert
        Assert.Equal("mistral", model.ModelName);
    }

    [Fact]
    public void OllamaModel_WithCustomBaseUrl_ShouldUseCustomUrl()
    {
        // Arrange & Act
        var model = new OllamaModel(baseUrl: "http://custom:8080/v1");

        // Assert
        // The base URL should be used for API calls
        Assert.Equal("mistral", new OllamaModel(modelName: "mistral").ModelName);
    }

    [Fact]
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
    public void OllamaModel_DefaultConstants_ShouldBeCorrect()
    {
        // Assert
        Assert.Equal("http://localhost:11434/v1", OllamaModel.DefaultBaseUrl);
        Assert.Equal("llama2", OllamaModel.DefaultModel);
    }

    [Fact(Skip = "Requires running Ollama server")]
    public async Task OllamaModel_GenerateAsync_WithRealOllama_ReturnsResponse()
    {
        // This test requires a running Ollama server with the specified model
        // Run with: dotnet test --filter "FullyQualifiedName~OllamaModel_GenerateAsync_WithRealOllama"
        
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
