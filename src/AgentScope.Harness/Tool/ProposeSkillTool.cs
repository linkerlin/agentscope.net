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

using AgentScope.Core.Tool;

namespace AgentScope.Harness.Tool;

/// <summary>
/// 技能提议工具：让 Agent 提议一个新技能草稿（写入草稿目录，按需安全扫描/审批）。
/// 对应 Java: io.agentscope.harness.agent.tool.ProposeSkillTool
/// </summary>
public sealed class ProposeSkillTool : ITool
{
    private readonly SkillManageConfig _config;
    private readonly Func<string, string, Task> _writer;

    /// <param name="writer">把（技能文件名, Markdown 内容）写入草稿目录的回调。</param>
    /// <param name="config">技能管理配置。</param>
    public ProposeSkillTool(Func<string, string, Task> writer, SkillManageConfig? config = null)
    {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _config = config ?? SkillManageConfig.Default;
    }

    public string Name => "propose_skill";
    public string Description => "提议一个新技能草稿（Markdown），写入草稿目录待审批";

    public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
    {
        if (!_config.AllowPropose)
        {
            return ToolResult.Fail("当前不允许提议新技能（AllowPropose=false）");
        }

        var name = parameters.GetValueOrDefault("name")?.ToString();
        var content = parameters.GetValueOrDefault("content")?.ToString();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(content))
        {
            return ToolResult.Fail("需要 name 与 content 参数");
        }

        // 安全扫描（尽力而为，不可作为唯一防线）：
        // 此处仅为基于关键字的浅层拦截，易通过空格/引号/变量拼接绕过。
        // 真正的安全保证应依赖 RequireApproval 的人工二次审批与白名单机制。
        if (_config.SecurityScanOnPropose &&
            Core.Tool.ToolDangerousPathConstants.ContainsDangerousCommand(content))
        {
            return ToolResult.Fail("提议内容包含危险命令关键字，已被安全策略拒绝");
        }

        var fileName = $"{name.Trim()}.md";
        try
        {
            await _writer(fileName, content);
        }
        catch (System.Exception ex)
        {
            return ToolResult.Fail($"写入草稿失败: {ex.Message}");
        }

        var status = _config.RequireApproval ? "（待审批）" : "（已自动启用）";
        return ToolResult.Ok($"技能草稿 '{fileName}' 已创建{status}");
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
                ["name"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "技能名称" },
                ["content"] = new Dictionary<string, object> { ["type"] = "string", ["description"] = "技能 Markdown 内容" }
            },
            ["required"] = new[] { "name", "content" }
        }
    };
}
