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
using System.Reflection;
using System.Threading.Tasks;
using AgentScope.Core.Formatter;
using AgentScope.Core.Message;

namespace AgentScope.Core.Tool
{
    /// <summary>
    /// 工具中心门面：统一注册、分组、激活、schema 聚合与技能组管理。
    /// 对标 Java: io.agentscope.core.tool.Toolkit
    /// </summary>
    public class Toolkit
    {
        private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, ToolGroup> _groups = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<SkillToolGroup> _skillGroups = new();

        public IReadOnlyCollection<ITool> AllTools => _tools.Values.ToList();
        public IReadOnlyCollection<ToolGroup> Groups => _groups.Values.ToList();

        public Toolkit AddTool(ITool tool, string? group = null)
        {
            if (tool is null)
            {
                throw new ArgumentNullException(nameof(tool));
            }

            _tools[tool.Name] = tool;

            if (!string.IsNullOrEmpty(group))
            {
                if (!_groups.TryGetValue(group!, out var toolGroup))
                {
                    toolGroup = new ToolGroup(group!, string.Empty);
                    _groups[group!] = toolGroup;
                }

                toolGroup.AddTool(tool.Name);
            }

            return this;
        }

        public Toolkit AddGroup(ToolGroup group)
        {
            if (group is null)
            {
                throw new ArgumentNullException(nameof(group));
            }

            _groups[group.Name] = group;
            return this;
        }

        public Toolkit AddSkillGroup(SkillToolGroup skillGroup)
        {
            if (skillGroup is null)
            {
                throw new ArgumentNullException(nameof(skillGroup));
            }

            _skillGroups.Add(skillGroup);

            foreach (var tool in skillGroup.Tools)
            {
                _tools[tool.Name] = tool;
            }

            return this;
        }

        public Toolkit ActivateGroup(string name)
        {
            if (_groups.TryGetValue(name, out var group))
            {
                group.IsActive = true;
            }

            return this;
        }

        public Toolkit DeactivateGroup(string name)
        {
            if (_groups.TryGetValue(name, out var group))
            {
                group.IsActive = false;
            }

            return this;
        }

        public IReadOnlyList<ITool> GetActiveTools()
        {
            var activeGroups = _groups.Values.Where(g => g.IsActive).ToList();

            if (activeGroups.Count == 0)
            {
                return _tools.Values.ToList();
            }

            var activeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in activeGroups)
            {
                foreach (var toolName in group.GetTools())
                {
                    activeNames.Add(toolName);
                }
            }

            var result = new List<ITool>();
            foreach (var name in activeNames)
            {
                if (_tools.TryGetValue(name, out var tool))
                {
                    result.Add(tool);
                }
            }

            return result;
        }

        public List<Dictionary<string, object>> GetActiveToolSchemas()
        {
            return GetActiveTools().Select(t => t.GetSchema()).ToList();
        }

        public ITool? Resolve(string name)
        {
            if (_tools.TryGetValue(name, out var tool))
            {
                return tool;
            }

            return null;
        }

        /// <summary>
        /// 通过反射扫描对象上的 [Tool] 方法注册工具
        /// </summary>
        public Toolkit RegisterTool(object toolObject)
        {
            if (toolObject is null) throw new ArgumentNullException(nameof(toolObject));

            var type = toolObject.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

            foreach (var method in methods)
            {
                var toolAttr = method.GetCustomAttribute<ToolAttribute>();
                if (toolAttr == null) continue;

                var toolName = toolAttr.Name ?? method.Name;
                var description = toolAttr.Description ?? $"[Tool] {type.Name}.{method.Name}";

                // 解析参数
                var parameters = new Dictionary<string, object>();
                var required = new List<string>();
                var methodParams = method.GetParameters();

                foreach (var param in methodParams)
                {
                    var paramAttr = param.GetCustomAttribute<ToolParamAttribute>();
                    var paramName = paramAttr?.Name ?? param.Name ?? "arg";
                    var paramDesc = paramAttr?.Description ?? $"Parameter {paramName}";
                    var isRequired = paramAttr?.Required ?? true;

                    var prop = new Dictionary<string, object>
                    {
                        ["type"] = GetJsonType(param.ParameterType),
                        ["description"] = paramDesc
                    };
                    parameters[paramName] = prop;
                    if (isRequired) required.Add(paramName);
                }

                var schema = new Dictionary<string, object>
                {
                    ["name"] = toolName,
                    ["description"] = description,
                    ["parameters"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = parameters,
                        ["required"] = required
                    }
                };

                // 创建反射调用包装工具
                var wrapper = new ReflectiveTool(toolName, description, schema, toolObject, method);
                _tools[toolName] = wrapper;
            }

            return this;
        }

        /// <summary>
        /// 泛型版本扫描类型 T 上的静态 [Tool] 方法
        /// </summary>
        public Toolkit RegisterTool<T>() where T : class
        {
            // 对于静态方法，传递 null 实例
            var type = typeof(T);
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            foreach (var method in methods)
            {
                var toolAttr = method.GetCustomAttribute<ToolAttribute>();
                if (toolAttr == null) continue;

                var toolName = toolAttr.Name ?? $"{type.Name}.{method.Name}";
                var description = toolAttr.Description ?? $"[Tool] {type.Name}.{method.Name}";

                var parameters = new Dictionary<string, object>();
                var required = new List<string>();
                var methodParams = method.GetParameters();

                foreach (var param in methodParams)
                {
                    var paramAttr = param.GetCustomAttribute<ToolParamAttribute>();
                    var paramName = paramAttr?.Name ?? param.Name ?? "arg";
                    var paramDesc = paramAttr?.Description ?? $"Parameter {paramName}";
                    var isRequired = paramAttr?.Required ?? true;

                    parameters[paramName] = new Dictionary<string, object>
                    {
                        ["type"] = GetJsonType(param.ParameterType),
                        ["description"] = paramDesc
                    };
                    if (isRequired) required.Add(paramName);
                }

                var schema = new Dictionary<string, object>
                {
                    ["name"] = toolName,
                    ["description"] = description,
                    ["parameters"] = new Dictionary<string, object>
                    {
                        ["type"] = "object",
                        ["properties"] = parameters,
                        ["required"] = required
                    }
                };

                var wrapper = new ReflectiveTool(toolName, description, schema, null, method);
                _tools[toolName] = wrapper;
            }

            return this;
        }

        /// <summary>
        /// 批量执行工具调用
        /// </summary>
        public async Task<List<ToolResultBlock>> CallToolsAsync(List<ToolUseBlock> toolCalls, ExecutionConfig? config = null)
        {
            var results = new List<ToolResultBlock>();
            foreach (var call in toolCalls)
            {
                if (_tools.TryGetValue(call.Name, out var tool))
                {
                    try
                    {
                        var result = await tool.ExecuteAsync(call.Input ?? new Dictionary<string, object>());
                        results.Add(new ToolResultBlock
                        {
                            Id = call.Id,
                            Output = result.Result,
                            IsError = !result.Success
                        });
                    }
                    catch (System.Exception ex)
                    {
                        results.Add(new ToolResultBlock
                        {
                            Id = call.Id,
                            Output = ex.Message,
                            IsError = true
                        });
                    }
                }
                else
                {
                    results.Add(new ToolResultBlock
                    {
                        Id = call.Id,
                        Output = $"Unknown tool: {call.Name}",
                        IsError = true
                    });
                }
            }
            return results;
        }

        /// <summary>
        /// Deep copy（供 Builder 使用）
        /// </summary>
        public Toolkit Copy()
        {
            var copy = new Toolkit();
            foreach (var kv in _tools)
            {
                copy._tools[kv.Key] = kv.Value;
            }
            foreach (var kv in _groups)
            {
                copy._groups[kv.Key] = new ToolGroup(kv.Value.Name, kv.Value.Description);
            }
            copy._skillGroups.AddRange(_skillGroups);
            return copy;
        }

        private static string GetJsonType(Type type)
        {
            if (type == typeof(string)) return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(double) || type == typeof(float))
                return "number";
            if (type == typeof(bool)) return "boolean";
            if (type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                return "array";
            return "string";
        }
    }

    /// <summary>
    /// 通过反射调用方法的工具包装器
    /// </summary>
    internal class ReflectiveTool : ITool
    {
        private readonly object? _instance;
        private readonly MethodInfo _method;

        public string Name { get; }
        public string Description { get; }
        public Dictionary<string, object> Schema { get; }

        public ReflectiveTool(string name, string description, Dictionary<string, object> schema, object? instance, MethodInfo method)
        {
            Name = name;
            Description = description;
            Schema = schema;
            _instance = instance;
            _method = method;
        }

        public async Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
        {
            try
            {
                var methodParams = _method.GetParameters();
                var args = new object?[methodParams.Length];

                for (int i = 0; i < methodParams.Length; i++)
                {
                    var paramName = methodParams[i].Name ?? $"arg{i}";
                    if (parameters.TryGetValue(paramName, out var val))
                    {
                        args[i] = ConvertValue(val, methodParams[i].ParameterType);
                    }
                    else
                    {
                        args[i] = methodParams[i].DefaultValue;
                    }
                }

                var result = _method.Invoke(_instance, args);

                // 处理 async Task<T> 返回
                if (result is Task taskResult)
                {
                    await taskResult.ConfigureAwait(false);
                    var resultProp = taskResult.GetType().GetProperty("Result");
                    if (resultProp != null)
                    {
                        return ToolResult.Ok(resultProp.GetValue(taskResult)!);
                    }
                    return ToolResult.Ok("ok");
                }

                return ToolResult.Ok(result!);
            }
            catch (System.Exception ex)
            {
                return ToolResult.Fail(ex.InnerException?.Message ?? ex.Message);
            }
        }

        public Dictionary<string, object> GetSchema() => Schema;

        private static object? ConvertValue(object val, Type targetType)
        {
            if (targetType == typeof(string)) return val?.ToString();
            if (targetType == typeof(int)) return Convert.ToInt32(val);
            if (targetType == typeof(long)) return Convert.ToInt64(val);
            if (targetType == typeof(double)) return Convert.ToDouble(val);
            if (targetType == typeof(bool)) return Convert.ToBoolean(val);
            return val;
        }
    }
}
