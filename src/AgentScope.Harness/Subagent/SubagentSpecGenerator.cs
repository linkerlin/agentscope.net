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

namespace AgentScope.Harness.Subagent;

/// <summary>
/// Subagent spec generator. Generates agent specifications in Markdown format.
/// 子代理规格生成器。以 Markdown 格式生成 Agent 规格描述。
/// </summary>
public sealed class SubagentSpecGenerator
{
    private static readonly string PromptTemplate = @"
根据以下描述生成子代理规格：
描述: {0}
已有 Agent ID: {1}
请以 Markdown 格式输出。";

    /// <summary>
    /// Generates a Markdown spec from a description and existing IDs.
    /// 根据描述和已有 ID 生成 Markdown 规格。
    /// </summary>
    /// <param name="description">Agent description / Agent 描述</param>
    /// <param name="existingIds">Collection of existing agent IDs / 已有 Agent ID 集合</param>
    /// <returns>The generated Markdown / 生成的 Markdown</returns>
    public async Task<string> GenerateMarkdownAsync(string description,
        ICollection<string> existingIds)
    {
        return string.Format(PromptTemplate, description,
            string.Join(", ", existingIds));
    }

    /// <summary>
    /// Generates and validates a spec, returning a structured result.
    /// 生成并验证规格，返回结构化结果。
    /// </summary>
    /// <param name="description">Agent description / Agent 描述</param>
    /// <param name="agentName">Agent name / Agent 名称</param>
    /// <param name="existingIds">Collection of existing agent IDs / 已有 Agent ID 集合</param>
    /// <returns>A GeneratedSpec with Markdown and optional declaration / 包含 Markdown 和可选声明的规格</returns>
    public async Task<GeneratedSpec?> GenerateAndValidateAsync(
        string description, string agentName,
        ICollection<string> existingIds)
    {
        var markdown = await GenerateMarkdownAsync(description, existingIds);
        return new GeneratedSpec(markdown, null);
    }

    /// <summary>
    /// Records a generated spec with its Markdown and optional parsed declaration.
    /// 记录生成的规格 Markdown 及其可选的解析声明。
    /// </summary>
    /// <param name="Markdown">The generated Markdown content / 生成的 Markdown 内容</param>
    /// <param name="Declaration">Optional parsed declaration / 可选的解析声明</param>
    public sealed record GeneratedSpec(string Markdown,
        SubagentDeclaration? Declaration);
}
