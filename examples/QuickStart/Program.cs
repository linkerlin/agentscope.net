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
using System.IO;
using System.Threading.Tasks;
using AgentScope.Core;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Model.DeepSeek;
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Memory;
using CoreVersion = AgentScope.Core.Version;
using DotNetEnv;

namespace QuickStart;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine($"{CoreVersion.GetFullVersion()}\n");
        Console.WriteLine("快速入门示例 - 简单 Agent 聊天\n");

        // 加载 .env 文件
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        // 从环境变量配置模型
        // 优先级：DeepSeek > OpenAI Compatible > MockModel
        IModel model;
        var deepseekApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        var deepseekModel = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL");
        var openaiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var openaiBaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        var openaiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");

        if (!string.IsNullOrEmpty(deepseekApiKey) && !string.IsNullOrEmpty(deepseekModel))
        {
            // 使用 DeepSeek
            Console.WriteLine($"使用 DeepSeek 模型：{deepseekModel}\n");
            model = DeepSeekModel.Builder()
                .ModelName(deepseekModel)
                .ApiKey(deepseekApiKey)
                .Build();
        }
        else if (!string.IsNullOrEmpty(openaiApiKey))
        {
            // 使用 OpenAI 兼容 API
            var modelName = openaiModel ?? "gpt-3.5-turbo";
            Console.WriteLine($"使用 OpenAI 兼容模型：{modelName}\n");
            model = new OpenAIModel(modelName, openaiApiKey, openaiBaseUrl);
        }
        else
        {
            // 回退到 MockModel
            Console.WriteLine("未找到 LLM API 密钥，使用 MockModel 进行测试。\n");
            Console.WriteLine("设置 DEEPSEEK_API_KEY 或 OPENAI_API_KEY 以使用真实 LLM。\n");
            model = MockModel.Builder()
                .ModelName("mock-model")
                .Build();
        }

        // 创建持久化记忆
        var memory = new SqliteMemory("example.db");

        // 创建 ReActAgent
        var agent = ReActAgent.Builder()
            .Name("Assistant")
            .SysPrompt("You are a helpful AI assistant.")
            .Model(model)
            .Memory(memory)
            .Build();

        Console.WriteLine("Agent 创建成功！");
        Console.WriteLine($"Agent 名称：{agent.Name}");
        Console.WriteLine($"记忆数量：{memory.Count()}\n");

        // 简单对话循环
        Console.WriteLine("输入您的消息（或输入 'quit' 退出）：\n");

        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "quit")
            {
                break;
            }

            // 创建用户消息
            var userMsg = Msg.Builder()
                .Role("user")
                .TextContent(input)
                .Build();

            // 调用 Agent
            var response = await agent.CallAsync(userMsg);
            var responseText = response.GetTextContent();

            Console.WriteLine($"Assistant: {responseText}\n");
        }

        Console.WriteLine("\n再见！");
        Console.WriteLine($"记忆中的消息总数：{memory.Count()}");
    }
}
