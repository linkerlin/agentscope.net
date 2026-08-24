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

using System.Collections.Generic;

namespace AgentScope.Core.Util;

/// <summary>
/// JSON Schema 工具：构造/校验最小 JSON Schema 片段。
/// 对应 Java: io.agentscope.core.util.JsonSchemaUtils
/// </summary>
public static class JsonSchemaUtils
{
    /// <summary>构造一个对象 schema。</summary>
    public static Dictionary<string, object> ObjectSchema(
        Dictionary<string, object>? properties = null,
        IList<string>? required = null,
        string? description = null)
    {
        var schema = new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = properties ?? new Dictionary<string, object>()
        };
        if (required != null && required.Count > 0) schema["required"] = required;
        if (!string.IsNullOrEmpty(description)) schema["description"] = description!;
        return schema;
    }

    /// <summary>构造一个数组 schema。</summary>
    public static Dictionary<string, object> ArraySchema(object items, string? description = null)
    {
        var schema = new Dictionary<string, object>
        {
            ["type"] = "array",
            ["items"] = items
        };
        if (!string.IsNullOrEmpty(description)) schema["description"] = description!;
        return schema;
    }

    /// <summary>构造一个基本类型 schema。</summary>
    public static Dictionary<string, object> Primitive(string type, string? description = null,
        IList<string>? enumValues = null)
    {
        var schema = new Dictionary<string, object> { ["type"] = type };
        if (!string.IsNullOrEmpty(description)) schema["description"] = description!;
        if (enumValues != null && enumValues.Count > 0) schema["enum"] = new List<string>(enumValues);
        return schema;
    }
}
