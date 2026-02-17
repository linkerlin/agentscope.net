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
using AgentScope.Core.Model;
using AgentScope.Core.Message;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AgentScope.Core.Tests.Model;

public class MockModelTests
{
    [Fact]
    public async Task MockModel_GenerateAsync_ShouldReturnEchoResponse()
    {
        // Arrange
        var model = MockModel.Builder().ModelName("test-model").Build();
        var request = new ModelRequest
        {
            Messages = new System.Collections.Generic.List<Msg>
            {
                Msg.Builder().TextContent("Hello").Build()
            }
        };

        // Act
        var response = await model.GenerateAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Text);
        Assert.Contains("Hello", response.Text);
    }

    [Fact]
    public async Task MockModel_Generate_ShouldWorkAsObservable()
    {
        // Arrange
        var model = MockModel.Builder().ModelName("test-model").Build();
        var request = new ModelRequest
        {
            Messages = new System.Collections.Generic.List<Msg>
            {
                Msg.Builder().TextContent("Test").Build()
            }
        };

        // Act
        var response = await model.Generate(request).FirstOrDefaultAsync();

        // Assert
        Assert.NotNull(response);
        Assert.True(response.Success);
    }

    [Fact]
    public void MockModelBuilder_WithModelName_ShouldSetModelName()
    {
        // Arrange & Act
        var model = MockModel.Builder()
            .ModelName("custom-model")
            .Build();

        // Assert
        Assert.Equal("custom-model", model.ModelName);
    }

    [Fact]
    public async Task MockModel_WithEmptyMessages_ShouldReturnEmptyResponse()
    {
        // Arrange
        var model = MockModel.Builder().Build();
        var request = new ModelRequest
        {
            Messages = new System.Collections.Generic.List<Msg>()
        };

        // Act
        var response = await model.GenerateAsync(request);

        // Assert
        Assert.True(response.Success);
        Assert.NotNull(response.Text);
    }

    [Fact]
    public async Task MockModel_ResponseMetadata_ShouldContainModelInfo()
    {
        // Arrange
        var model = MockModel.Builder().ModelName("test-model").Build();
        var request = new ModelRequest
        {
            Messages = new System.Collections.Generic.List<Msg>
            {
                Msg.Builder().TextContent("Test").Build()
            }
        };

        // Act
        var response = await model.GenerateAsync(request);

        // Assert
        Assert.NotNull(response.Metadata);
        Assert.Contains("model", response.Metadata.Keys);
        Assert.Equal("test-model", response.Metadata["model"]);
    }
}
