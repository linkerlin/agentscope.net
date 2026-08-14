// Copyright 2024-2026 the original author or authors.
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

using AgentScope.Core.Tool;
using AgentScope.Harness.Workspace;

namespace AgentScope.Harness.Tool;

/// <summary>
/// 计划模式工具集：切换 PLAN/BUILD 模式、查询当前模式。
/// 对应 Java: io.agentscope.harness.agent.tool.PlanModeTools
/// </summary>
public static class PlanModeTools
{
    /// <summary>创建“切换模式”工具。</summary>
    public static ITool CreateToggleTool(PlanModeManager manager) => new TogglePlanModeTool(manager);

    /// <summary>创建“查询当前模式”工具。</summary>
    public static ITool CreateQueryTool(PlanModeManager manager) => new QueryPlanModeTool(manager);

    private sealed class TogglePlanModeTool : ITool
    {
        private readonly PlanModeManager _manager;
        public TogglePlanModeTool(PlanModeManager m) => _manager = m;
        public string Name => "plan_mode_toggle";
        public string Description => "切换 PLAN/BUILD 模式";

        public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            var target = parameters.GetValueOrDefault("mode")?.ToString();
            if (!string.IsNullOrEmpty(target) &&
                Enum.TryParse<PlanMode>(target, ignoreCase: true, out var mode))
            {
                _manager.SetMode(mode);
            }
            else
            {
                _manager.Toggle();
            }

            return Task.FromResult(ToolResult.Ok($"当前模式: {_manager.CurrentMode}"));
        }

        public Dictionary<string, object> GetSchema() => new()
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>
                {
                    ["mode"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "目标模式 Plan/Build（省略则切换）" }
                },
                ["required"] = Array.Empty<string>()
            }
        };
    }

    private sealed class QueryPlanModeTool : ITool
    {
        private readonly PlanModeManager _manager;
        public QueryPlanModeTool(PlanModeManager m) => _manager = m;
        public string Name => "plan_mode_query";
        public string Description => "查询当前 PLAN/BUILD 模式";

        public Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
            => Task.FromResult(ToolResult.Ok($"当前模式: {_manager.CurrentMode}"));

        public Dictionary<string, object> GetSchema() => new()
        {
            ["name"] = Name,
            ["description"] = Description,
            ["parameters"] = new Dictionary<string, object>
            {
                ["type"] = "object",
                ["properties"] = new Dictionary<string, object>(),
                ["required"] = Array.Empty<string>()
            }
        };
    }
}
