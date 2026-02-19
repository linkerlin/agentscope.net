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
        Console.WriteLine("QuickStart Example - Simple Agent Chat\n");

        // Load .env file
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        // Configure model from environment variables
        // Priority: DeepSeek > OpenAI Compatible > MockModel
        IModel model;
        var deepseekApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        var deepseekModel = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL");
        var openaiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var openaiBaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        var openaiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");

        if (!string.IsNullOrEmpty(deepseekApiKey) && !string.IsNullOrEmpty(deepseekModel))
        {
            // Use DeepSeek
            Console.WriteLine($"Using DeepSeek model: {deepseekModel}\n");
            model = DeepSeekModel.Builder()
                .ModelName(deepseekModel)
                .ApiKey(deepseekApiKey)
                .Build();
        }
        else if (!string.IsNullOrEmpty(openaiApiKey))
        {
            // Use OpenAI Compatible API
            var modelName = openaiModel ?? "gpt-3.5-turbo";
            Console.WriteLine($"Using OpenAI Compatible model: {modelName}\n");
            model = new OpenAIModel(modelName, openaiApiKey, openaiBaseUrl);
        }
        else
        {
            // Fallback to MockModel
            Console.WriteLine("No LLM API key found, using MockModel for testing.\n");
            Console.WriteLine("Set DEEPSEEK_API_KEY or OPENAI_API_KEY to use real LLM.\n");
            model = MockModel.Builder()
                .ModelName("mock-model")
                .Build();
        }

        // Create persistent memory
        var memory = new SqliteMemory("example.db");

        // Create ReActAgent
        var agent = ReActAgent.Builder()
            .Name("Assistant")
            .SysPrompt("You are a helpful AI assistant.")
            .Model(model)
            .Memory(memory)
            .Build();

        Console.WriteLine("Agent created successfully!");
        Console.WriteLine($"Agent Name: {agent.Name}");
        Console.WriteLine($"Memory Count: {memory.Count()}\n");

        // Simple conversation loop
        Console.WriteLine("Type your message (or 'quit' to exit):\n");

        while (true)
        {
            Console.Write("You: ");
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input) || input.ToLower() == "quit")
            {
                break;
            }

            // Create user message
            var userMsg = Msg.Builder()
                .Role("user")
                .TextContent(input)
                .Build();

            // Call agent
            var response = await agent.CallAsync(userMsg);
            var responseText = response.GetTextContent();

            Console.WriteLine($"Assistant: {responseText}\n");
        }

        Console.WriteLine("\nGoodbye!");
        Console.WriteLine($"Total messages in memory: {memory.Count()}");
    }
}
