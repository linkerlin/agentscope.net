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

namespace AgentScope.Core.Exception;

/// <summary>
/// Base exception for AgentScope
/// </summary>
public class AgentScopeException : System.Exception
{
    public AgentScopeException() { }
    
    public AgentScopeException(string message) : base(message) { }
    
    public AgentScopeException(string message, System.Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception for model errors
/// </summary>
public class ModelException : AgentScopeException
{
    public ModelException() { }
    
    public ModelException(string message) : base(message) { }
    
    public ModelException(string message, System.Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception for tool execution errors
/// </summary>
public class ToolException : AgentScopeException
{
    public ToolException() { }
    
    public ToolException(string message) : base(message) { }
    
    public ToolException(string message, System.Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception for agent errors
/// </summary>
public class AgentException : AgentScopeException
{
    public AgentException() { }
    
    public AgentException(string message) : base(message) { }
    
    public AgentException(string message, System.Exception inner) : base(message, inner) { }
}

/// <summary>
/// Exception for memory errors
/// </summary>
public class MemoryException : AgentScopeException
{
    public MemoryException() { }
    
    public MemoryException(string message) : base(message) { }
    
    public MemoryException(string message, System.Exception inner) : base(message, inner) { }
}
