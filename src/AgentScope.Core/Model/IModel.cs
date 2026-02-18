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
using AgentScope.Core.Message;

namespace AgentScope.Core.Model;

/// <summary>
/// 模型请求
/// Model request for LLM
/// </summary>
public class 模型请求
{
    public List<Msg> 消息列表 { get; set; } = new();
    public Dictionary<string, object>? 选项 { get; set; }
}

/// <summary>
/// 模型响应
/// Model response from LLM
/// </summary>
public class 模型响应
{
    public string? 文本内容 { get; set; }
    public Dictionary<string, object>? 元数据 { get; set; }
    public bool 成功 { get; set; }
    public string? 错误信息 { get; set; }
}

/// <summary>
/// 大语言模型接口
/// Interface for LLM models
/// </summary>
public interface I模型
{
    string 模型名称 { get; }
    
    IObservable<模型响应> 生成 (模型请求 request);
    
    Task<模型响应> 生成 Async(模型请求 request);
}

/// <summary>
/// 模型基类
/// Abstract base class for models
/// </summary>
public abstract class 模型基类 : I模型
{
    public string 模型名称 { get; protected set; }

    protected 模型基类 (string modelName)
    {
        模型名称 = modelName;
    }

    public abstract IObservable<模型响应> 生成 (模型请求 request);

    public abstract Task<模型响应> 生成 Async(模型请求 request);
}
