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
using Terminal.Gui;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Memory;
using CoreVersion = AgentScope.Core.Version;

namespace AgentScope.TUI;

class Program
{
    static void Main(string[] args)
    {
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

        // Initialize agent
        var memory = new SqliteMemory("agentscope.db");
        var model = MockModel.Builder().ModelName("mock-model").Build();
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
            "Type your message and press Enter or click Send to chat with the assistant.";

        Application.Run();
        Application.Shutdown();
    }
}
