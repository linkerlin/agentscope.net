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
/// Example tool that calculates the sum of two numbers
/// </summary>
public class CalculatorTool : ToolBase
{
    public CalculatorTool() : base("calculator", "Calculates the sum of two numbers")
    {
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "function",
            ["function"] = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>
                    {
                        ["a"] = new Dictionary<string, object>
                        {
                            ["type"] = "number",
                            ["description"] = "First number"
                        },
                        ["b"] = new Dictionary<string, object>
                        {
                            ["type"] = "number",
                            ["description"] = "Second number"
                        }
                    },
                    ["required"] = new[] { "a", "b" }
                }
            }
        };
    }

    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        try
        {
            if (!parameters.ContainsKey("a") || !parameters.ContainsKey("b"))
            {
                return Task.FromResult(ToolResult.Fail("Missing required parameters: a and b"));
            }

            var a = Convert.ToDouble(parameters["a"]);
            var b = Convert.ToDouble(parameters["b"]);
            var sum = a + b;

            return Task.FromResult(ToolResult.Ok(sum));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ToolResult.Fail($"Error: {ex.Message}"));
        }
    }
}

/// <summary>
/// Example tool that gets the current time
/// </summary>
public class GetTimeTool : ToolBase
{
    public GetTimeTool() : base("get_time", "Gets the current time")
    {
    }

    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "function",
            ["function"] = new Dictionary<string, object>
            {
                ["name"] = Name,
                ["description"] = Description,
                ["parameters"] = new Dictionary<string, object>
                {
                    ["type"] = "object",
                    ["properties"] = new Dictionary<string, object>(),
                    ["required"] = Array.Empty<string>()
                }
            }
        };
    }

    public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        return Task.FromResult(ToolResult.Ok(now));
    }
}
