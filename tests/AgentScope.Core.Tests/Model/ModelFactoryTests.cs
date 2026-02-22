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
using AgentScope.Core;
using AgentScope.Core.Model;
using System.Collections.Generic;
using System;

namespace AgentScope.Core.Tests.Model;

public class ModelFactoryTests
{
    [Fact]
    public void Create_OpenAI_ShouldReturnOpenAIModel()
    {
        var model = ModelFactory.Create("openai", "gpt-4o", "test-api-key");

        Assert.NotNull(model);
        Assert.Equal("gpt-4o", model.ModelName);
    }

    [Fact]
    public void Create_OpenAI_WithBaseUrl_ShouldReturnOpenAIModel()
    {
        var model = ModelFactory.Create("openai", "gpt-4o", "test-api-key", "https://custom.endpoint.com/v1");

        Assert.NotNull(model);
        Assert.Equal("gpt-4o", model.ModelName);
    }

    [Fact]
    public void Create_Azure_ShouldReturnOpenAIModel()
    {
        var model = ModelFactory.Create("azure", "gpt-4o", "test-api-key", "https://my-resource.openai.azure.com");

        Assert.NotNull(model);
        Assert.Equal("gpt-4o", model.ModelName);
    }

    [Fact]
    public void Create_Azure_MissingBaseUrl_ShouldThrow()
    {
        Assert.Throws<InvalidOperationException>(() =>
            ModelFactory.Create("azure", "gpt-4o", "test-api-key"));
    }

    [Fact]
    public void Create_Anthropic_ShouldReturnAnthropicModel()
    {
        var model = ModelFactory.Create("anthropic", "claude-sonnet-4-5-20250929", "test-api-key");

        Assert.NotNull(model);
        Assert.Equal("claude-sonnet-4-5-20250929", model.ModelName);
    }

    [Fact]
    public void Create_Anthropic_WithBaseUrl_ShouldReturnAnthropicModel()
    {
        var model = ModelFactory.Create("anthropic", "claude-sonnet-4-5-20250929", "test-api-key", "https://custom.anthropic.com");

        Assert.NotNull(model);
        Assert.Equal("claude-sonnet-4-5-20250929", model.ModelName);
    }

    [Fact]
    public void Create_DeepSeek_ShouldReturnDeepSeekModel()
    {
        var model = ModelFactory.Create("deepseek", "deepseek-chat", "test-api-key");

        Assert.NotNull(model);
        Assert.Equal("deepseek-chat", model.ModelName);
    }

    [Fact]
    public void Create_Gemini_ShouldReturnGeminiModel()
    {
        var model = ModelFactory.Create("gemini", "gemini-2.0-flash-exp", "test-api-key");

        Assert.NotNull(model);
        Assert.Equal("gemini-2.0-flash-exp", model.ModelName);
    }

    [Fact]
    public void Create_DashScope_ShouldReturnDashScopeModel()
    {
        var model = ModelFactory.Create("dashscope", "qwen-turbo", "test-api-key");

        Assert.NotNull(model);
        Assert.Equal("qwen-turbo", model.ModelName);
    }

    [Fact]
    public void Create_Ollama_ShouldReturnOllamaModel()
    {
        var model = ModelFactory.Create("ollama", "llama3", "dummy-key");

        Assert.NotNull(model);
        Assert.Equal("llama3", model.ModelName);
    }

    [Fact]
    public void Create_Ollama_WithBaseUrl_ShouldReturnOllamaModel()
    {
        var model = ModelFactory.Create("ollama", "llama3", "dummy-key", "http://localhost:11434");

        Assert.NotNull(model);
        Assert.Equal("llama3", model.ModelName);
    }

    [Fact]
    public void Create_UnsupportedProvider_ShouldThrow()
    {
        Assert.Throws<NotSupportedException>(() =>
            ModelFactory.Create("unknown", "model", "api-key"));
    }

    [Fact]
    public void Create_CaseInsensitive_ShouldWork()
    {
        var model1 = ModelFactory.Create("OPENAI", "gpt-4o", "test-key");
        var model2 = ModelFactory.Create("OpenAI", "gpt-4o", "test-key");
        var model3 = ModelFactory.Create("openai", "gpt-4o", "test-key");

        Assert.NotNull(model1);
        Assert.NotNull(model2);
        Assert.NotNull(model3);
    }

    [Fact]
    public void Create_WithDictConfig_ShouldWork()
    {
        var config = new Dictionary<string, string>
        {
            ["provider"] = "openai",
            ["model"] = "gpt-4o",
            ["apiKey"] = "test-key"
        };

        var model = ModelFactory.Create(config);

        Assert.NotNull(model);
        Assert.Equal("gpt-4o", model.ModelName);
    }

    [Fact]
    public void Create_WithDictConfig_MissingApiKey_ShouldThrow()
    {
        var config = new Dictionary<string, string>
        {
            ["provider"] = "openai",
            ["model"] = "gpt-4o"
        };

        Assert.Throws<InvalidOperationException>(() =>
            ModelFactory.Create(config));
    }

    [Fact]
    public void Create_WithDictConfig_WithBaseUrl_ShouldWork()
    {
        var config = new Dictionary<string, string>
        {
            ["provider"] = "openai",
            ["model"] = "gpt-4o",
            ["apiKey"] = "test-key",
            ["baseUrl"] = "https://custom.endpoint.com/v1"
        };

        var model = ModelFactory.Create(config);

        Assert.NotNull(model);
        Assert.Equal("gpt-4o", model.ModelName);
    }
}

public class ModelFactoryExtensionsTests
{
    [Theory]
    [InlineData("openai", true)]
    [InlineData("OpenAI", true)]
    [InlineData("OPENAI", true)]
    [InlineData("anthropic", true)]
    [InlineData("deepseek", true)]
    [InlineData("gemini", true)]
    [InlineData("dashscope", true)]
    [InlineData("ollama", true)]
    [InlineData("azure", true)]
    [InlineData("unknown", false)]
    public void IsSupportedProvider_ShouldReturnCorrectValue(string provider, bool expected)
    {
        var result = ModelFactoryExtensions.IsSupportedProvider(provider);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("openai", "gpt-4o")]
    [InlineData("azure", "gpt-4o")]
    [InlineData("anthropic", "claude-sonnet-4-5-20250929")]
    [InlineData("deepseek", "deepseek-chat")]
    [InlineData("gemini", "gemini-2.0-flash-exp")]
    [InlineData("dashscope", "qwen-turbo")]
    [InlineData("ollama", "llama3")]
    public void GetDefaultModel_ShouldReturnCorrectModel(string provider, string expectedModel)
    {
        var result = ModelFactoryExtensions.GetDefaultModel(provider);
        Assert.Equal(expectedModel, result);
    }

    [Fact]
    public void GetSupportedProviders_ShouldReturnAllProviders()
    {
        var providers = ModelFactoryExtensions.GetSupportedProviders();

        Assert.Contains("openai", providers);
        Assert.Contains("azure", providers);
        Assert.Contains("anthropic", providers);
        Assert.Contains("deepseek", providers);
        Assert.Contains("gemini", providers);
        Assert.Contains("dashscope", providers);
        Assert.Contains("ollama", providers);
        Assert.Equal(7, providers.Count);
    }
}
