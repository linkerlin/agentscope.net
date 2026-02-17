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

namespace AgentScope.Core.Tool;

/// <summary>
/// Tool result
/// </summary>
public class ToolResult
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? Error { get; set; }
    
    public static ToolResult Ok(object result)
    {
        return new ToolResult { Success = true, Result = result };
    }
    
    public static ToolResult Fail(string error)
    {
        return new ToolResult { Success = false, Error = error };
    }
}

/// <summary>
/// Interface for tools that agents can use
/// </summary>
public interface ITool
{
    string Name { get; }
    
    string Description { get; }
    
    Dictionary<string, object> GetSchema();
    
    Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
}

/// <summary>
/// Abstract base class for tools
/// </summary>
public abstract class ToolBase : ITool
{
    public string Name { get; protected set; }
    
    public string Description { get; protected set; }

    protected ToolBase(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public abstract Dictionary<string, object> GetSchema();

    public abstract Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters);
}
