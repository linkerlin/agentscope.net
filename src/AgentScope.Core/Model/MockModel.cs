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
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace AgentScope.Core.Model;

/// <summary>
/// Mock model for testing and examples
/// </summary>
public class MockModel : ModelBase
{
    public MockModel(string modelName = "mock-model") : base(modelName)
    {
    }

    public override IObservable<ModelResponse> Generate(ModelRequest request)
    {
        return Observable.FromAsync(() => GenerateAsync(request));
    }

    public override Task<ModelResponse> GenerateAsync(ModelRequest request)
    {
        var lastMessage = request.Messages.LastOrDefault();
        var text = lastMessage?.GetTextContent() ?? string.Empty;

        var response = new ModelResponse
        {
            Success = true,
            Text = $"Echo: {text}",
            Metadata = new System.Collections.Generic.Dictionary<string, object>
            {
                ["model"] = ModelName,
                ["timestamp"] = DateTime.UtcNow
            }
        };

        return Task.FromResult(response);
    }

    public static MockModelBuilder Builder()
    {
        return new MockModelBuilder();
    }
}

/// <summary>
/// Builder for MockModel
/// </summary>
public class MockModelBuilder
{
    private string _modelName = "mock-model";

    public MockModelBuilder ModelName(string name)
    {
        _modelName = name;
        return this;
    }

    public MockModel Build()
    {
        return new MockModel(_modelName);
    }
}
