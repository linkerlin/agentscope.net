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
using Terminal.Gui;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Model.DeepSeek;
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Memory;
using CoreVersion = AgentScope.Core.Version;
using DotNetEnv;

namespace AgentScope.TUI;

class Program
{
    static void Main(string[] args)
    {
        // Load .env file
        var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        Application.Init();

        var top = Application.Top;

        var win = new Window("AgentScope.NET - TUI Chat")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        var menuBar = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_File", new MenuItem[]
            {
                new MenuItem("_Quit", "", () => Application.RequestStop())
            }),
            new MenuBarItem("_Help", new MenuItem[]
            {
                new MenuItem("_About", "", () =>
                {
                    MessageBox.Query("About", $"{CoreVersion.GetFullVersion()}\n\n" +
                        "A .NET port of AgentScope framework\n" +
                        "for building LLM-powered applications.", "Ok");
                })
            })
        });

        var chatView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 3,
            ReadOnly = true
        };

        var inputLabel = new Label("Input: ")
        {
            X = 0,
            Y = Pos.Bottom(chatView)
        };

        var inputField = new TextField("")
        {
            X = Pos.Right(inputLabel),
            Y = Pos.Bottom(chatView),
            Width = Dim.Fill() - 10
        };

        var sendButton = new Button("Send")
        {
            X = Pos.Right(inputField) + 1,
            Y = Pos.Bottom(chatView)
        };

        // Initialize model from environment variables
        // Priority: DeepSeek > OpenAI Compatible > MockModel
        IModel model;
        string modelInfo;
        var deepseekApiKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        var deepseekModel = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL");
        var openaiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var openaiBaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        var openaiModel = Environment.GetEnvironmentVariable("OPENAI_MODEL");

        if (!string.IsNullOrEmpty(deepseekApiKey) && !string.IsNullOrEmpty(deepseekModel))
        {
            // Use DeepSeek
            modelInfo = $"DeepSeek: {deepseekModel}";
            model = DeepSeekModel.Builder()
                .ModelName(deepseekModel)
                .ApiKey(deepseekApiKey)
                .Build();
        }
        else if (!string.IsNullOrEmpty(openaiApiKey))
        {
            // Use OpenAI Compatible API
            var modelName = openaiModel ?? "gpt-3.5-turbo";
            modelInfo = $"OpenAI: {modelName}";
            model = new OpenAIModel(modelName, openaiApiKey, openaiBaseUrl);
        }
        else
        {
            // Fallback to MockModel
            modelInfo = "MockModel (test mode)";
            model = MockModel.Builder().ModelName("mock-model").Build();
        }

        // Initialize agent
        var memory = new SqliteMemory("agentscope.db");
        var agent = Core.ReActAgent.Builder()
            .Name("Assistant")
            .SysPrompt("You are a helpful AI assistant.")
            .Model(model)
            .Memory(memory)
            .Build();

        Action sendAction = async () =>
        {
            var input = inputField.Text.ToString();
            if (string.IsNullOrWhiteSpace(input))
                return;

            chatView.Text += $"\n\nUser: {input}";
            inputField.Text = "";

            try
            {
                var userMsg = Msg.Builder()
                    .Role("user")
                    .TextContent(input)
                    .Build();

                var response = await agent.CallAsync(userMsg);
                var responseText = response.GetTextContent();

                chatView.Text += $"\n\nAssistant: {responseText}";
                chatView.MoveEnd();
            }
            catch (Exception ex)
            {
                chatView.Text += $"\n\nError: {ex.Message}";
            }
        };

        sendButton.Clicked += () => sendAction();

        inputField.KeyPress += (e) =>
        {
            if (e.KeyEvent.Key == Key.Enter)
            {
                sendAction();
                e.Handled = true;
            }
        };

        win.Add(chatView, inputLabel, inputField, sendButton);
        top.Add(menuBar, win);

        chatView.Text = $"Welcome to {CoreVersion.GetFullVersion()}!\n\n" +
            $"Model: {modelInfo}\n\n" +
            "Type your message and press Enter or click Send to chat with the assistant.";

        Application.Run();
        Application.Shutdown();
    }
}
