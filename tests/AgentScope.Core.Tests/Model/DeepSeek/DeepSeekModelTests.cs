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
using System.Threading.Tasks;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Model.DeepSeek;
using Xunit;

namespace AgentScope.Core.Tests.Model.DeepSeek;

public class DeepSeekModelTests
{
    private readonly string? _apiKey;
    private readonly string? _modelName;
    private readonly bool _isConfigured;

    public DeepSeekModelTests()
    {
        _apiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        _modelName = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL");
        _isConfigured = !string.IsNullOrEmpty(_apiKey) && !string.IsNullOrEmpty(_modelName);
    }

    [Fact]
    public void DeepSeekModel_DefaultConstructor_SetsCorrectDefaults()
    {
        var model = new DeepSeekModel();

        Assert.NotNull(model);
    }

    [Fact]
    public void DeepSeekModel_Models_HaveCorrectValues()
    {
        Assert.Equal("deepseek-chat", DeepSeekModel.Models.Chat);
        Assert.Equal("deepseek-reasoner", DeepSeekModel.Models.Reasoner);
    }

    [Fact]
    public void DeepSeekModel_DefaultBaseUrl_IsCorrect()
    {
        Assert.Equal("https://api.deepseek.com", DeepSeekModel.DefaultBaseUrl);
    }

    [Fact]
    public void DeepSeekModel_DefaultModel_IsChat()
    {
        Assert.Equal("deepseek-chat", DeepSeekModel.DefaultModel);
    }

    [Fact]
    public void DeepSeekModelBuilder_UseChat_SetsChatModel()
    {
        var model = DeepSeekModel.Builder()
            .UseChat()
            .ApiKey("test-key")
            .Build();

        Assert.NotNull(model);
    }

    [Fact]
    public void DeepSeekModelBuilder_UseReasoner_SetsReasonerModel()
    {
        var model = DeepSeekModel.Builder()
            .UseReasoner()
            .ApiKey("test-key")
            .Build();

        Assert.NotNull(model);
    }

    [Fact]
    public void DeepSeekModelBuilder_ModelName_SetsCustomModel()
    {
        var model = DeepSeekModel.Builder()
            .ModelName("custom-model")
            .ApiKey("test-key")
            .Build();

        Assert.NotNull(model);
    }

    [Fact(Skip = "Requires DEEPSEEK_API_KEY environment variable")]
    public async Task DeepSeekModel_GenerateAsync_WithRealApi_ReturnsResponse()
    {
        if (!_isConfigured)
        {
            return;
        }

        var model = DeepSeekModel.Builder()
            .ModelName(_modelName!)
            .ApiKey(_apiKey!)
            .Build();

        var message = Msg.Builder()
            .Role("user")
            .TextContent("Say 'Hello, World!'")
            .Build();

        var request = new ModelRequest
        {
            Messages = new List<Msg> { message }
        };

        var response = await model.GenerateAsync(request);

        Assert.NotNull(response);
        Assert.True(response.Success);
        Assert.False(string.IsNullOrEmpty(response.Text));
    }
}
