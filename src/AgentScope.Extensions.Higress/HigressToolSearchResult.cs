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

using System.Text.Json;

namespace AgentScope.Extensions.Higress;

/// <summary>
/// Higress 工具搜索结果：名称、描述、参数 schema 与路由信息。
/// 对应 Java: io.agentscope.extensions.higress.HigressToolSearchResult
/// </summary>
public sealed class HigressToolSearchResult
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public JsonElement? ParametersSchema { get; set; }
    public string? Route { get; set; }
    public double Score { get; set; }

    public override string ToString() => $"{Name} ({Score:F2}): {Description}";
}
