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
using System.Linq;
using AgentScope.Core.Tool;
using AgentScope.Core.Tool.Coding;
using AgentScope.Core.Tool.File;

namespace AgentScope.Core;

public enum ToolPreset
{
    Default,
    Advanced,
    All
}

public static class ToolFactory
{
    private static readonly IReadOnlyDictionary<string, Func<Dictionary<string, object>?, ITool>> ToolConstructors =
        new Dictionary<string, Func<Dictionary<string, object>?, ITool>>(StringComparer.OrdinalIgnoreCase)
        {
            ["calculator"] = _ => new CalculatorTool(),
            ["get_time"] = _ => new GetTimeTool(),
            ["web_search"] = _ => new WebSearchTool(),
            ["code_execution"] = _ => new CodeExecutionTool(),
            ["read_file"] = _ => new ReadFileTool(),
            ["write_file"] = _ => new WriteFileTool(),
            ["shell_command"] = _ => new ShellCommandTool()
        };

    private static readonly IReadOnlyList<string> DefaultToolTypes =
        new[] { "calculator", "get_time", "web_search", "code_execution" };

    private static readonly IReadOnlyList<string> AdvancedToolTypes =
        new[] { "read_file", "write_file", "shell_command" };

    public static ITool Create(string toolType, Dictionary<string, object>? config = null)
    {
        if (string.IsNullOrWhiteSpace(toolType))
        {
            throw new NotSupportedException("Tool type '' is not supported");
        }

        if (ToolConstructors.TryGetValue(toolType.Trim(), out var factory))
        {
            return factory(config);
        }

        throw new NotSupportedException($"Tool type '{toolType}' is not supported");
    }

    public static List<ITool> CreateDefaults()
    {
        return CreatePreset(ToolPreset.Default);
    }

    public static List<ITool> CreateAdvanced()
    {
        return CreatePreset(ToolPreset.Advanced);
    }

    public static List<ITool> CreateAll()
    {
        return CreatePreset(ToolPreset.All);
    }

    public static List<ITool> CreatePreset(ToolPreset preset)
    {
        return GetToolNames(preset)
            .Select(name => Create(name))
            .ToList();
    }

    public static List<string> GetToolNames(ToolPreset preset)
    {
        return preset switch
        {
            ToolPreset.Default => DefaultToolTypes.ToList(),
            ToolPreset.Advanced => AdvancedToolTypes.ToList(),
            ToolPreset.All => DefaultToolTypes.Concat(AdvancedToolTypes).ToList(),
            _ => throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown tool preset")
        };
    }
}

public static class ToolFactoryExtensions
{
    public static bool IsSupportedTool(string toolType)
    {
        if (string.IsNullOrWhiteSpace(toolType))
            return false;

        return ToolFactory.GetToolNames(ToolPreset.All)
            .Contains(toolType.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsDefaultTool(string toolType)
    {
        if (string.IsNullOrWhiteSpace(toolType))
            return false;

        return ToolFactory.GetToolNames(ToolPreset.Default)
            .Contains(toolType.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsAdvancedTool(string toolType)
    {
        if (string.IsNullOrWhiteSpace(toolType))
            return false;

        return ToolFactory.GetToolNames(ToolPreset.Advanced)
            .Contains(toolType.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static List<string> GetSupportedTools()
    {
        return ToolFactory.GetToolNames(ToolPreset.All);
    }

    public static List<string> GetDefaultTools()
    {
        return ToolFactory.GetToolNames(ToolPreset.Default);
    }

    public static List<string> GetAdvancedTools()
    {
        return ToolFactory.GetToolNames(ToolPreset.Advanced);
    }
}
