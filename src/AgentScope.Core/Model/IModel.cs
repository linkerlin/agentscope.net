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

namespace AgentScope.Core.Model;

/// <summary>
/// Model request for LLM
/// </summary>
public class ModelRequest
{
    public List<Msg> Messages { get; set; } = new();
    public Dictionary<string, object>? Options { get; set; }
}

/// <summary>
/// Model response from LLM
/// </summary>
public class ModelResponse
{
    public string? Text { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Interface for LLM models
/// </summary>
public interface IModel
{
    string ModelName { get; }
    
    IObservable<ModelResponse> Generate(ModelRequest request);
    
    Task<ModelResponse> GenerateAsync(ModelRequest request);
}

/// <summary>
/// Abstract base class for models
/// </summary>
public abstract class ModelBase : IModel
{
    private readonly string _modelName;
    
    public string ModelName 
    { 
        get => _modelName;
        protected set => throw new InvalidOperationException("ModelName cannot be changed after construction");
    }

    protected ModelBase(string modelName)
    {
        _modelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
    }

    public abstract IObservable<ModelResponse> Generate(ModelRequest request);

    public abstract Task<ModelResponse> GenerateAsync(ModelRequest request);
}
