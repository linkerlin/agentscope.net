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

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AgentScope.Core;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Memory;
using AgentScope.Core.Configuration;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace AgentScope.Uno;

/// <summary>
/// AgentScope.NET 主窗口
/// Main window for AgentScope.NET application
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>
    /// Agent 实例引用
    /// Agent instance reference
    /// </summary>
    private EnhancedReActAgent? _agent;

    /// <summary>
    /// 记忆存储实例
    /// Memory storage instance
    /// </summary>
    private IMemory? _memory;

    /// <summary>
    /// 聊天消息集合（用于 UI 绑定）
    /// Chat message collection (for UI binding)
    /// </summary>
    private ObservableCollection<ChatMessage> _messages;

    /// <summary>
    /// 初始化主窗口并初始化 Agent
    /// Initializes the main window and initializes the agent
    /// </summary>
    public MainWindow()
    {
        this.InitializeComponent();
        this.Title = "AgentScope.NET - Uno Platform";
        
        _messages = new ObservableCollection<ChatMessage>();
        
        // 初始化 Agent Initialize agent
        InitializeAgent();
    }

    /// <summary>
    /// 初始化 Agent - 创建内存存储、配置模型并构建 Agent
    /// Initializes the agent - creates memory storage, configures model and builds the agent
    /// </summary>
    private void InitializeAgent()
    {
        try
        {
            // 使用 SQLite 内存 Use SQLite memory
            var dbPath = ConfigurationManager.GetDatabasePath();
            _memory = new SqliteMemory(dbPath);

            // 创建模型（暂时使用 Mock Model，后续会替换为真实 LLM）
            // Create model (using Mock Model for now, will be replaced with real LLM)
            var model = MockModel.Builder().ModelName("mock-model").Build();

            // 创建 Agent Create agent
            _agent = EnhancedReActAgent.Builder()
                .Name("Assistant")
                .Model(model)
                .Memory(_memory)
                .SysPrompt("你是一个有帮助的AI助手。You are a helpful AI assistant.")
                .Build();

            AddSystemMessage("Agent 已初始化。Agent initialized.");
        }
        catch (System.Exception ex)
        {
            AddSystemMessage($"初始化错误 Initialization error: {ex.Message}");
        }
    }

    /// <summary>
    /// 发送按钮点击事件处理
    /// Send button click event handler
    /// </summary>
    /// <param name="sender">事件发送者 / Event sender</param>
    /// <param name="e">路由事件参数 / Routed event arguments</param>
    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        await SendMessageAsync();
    }

    /// <summary>
    /// 输入框按键事件处理（支持 Enter 键发送）
    /// Input box key down event handler (supports Enter key to send)
    /// </summary>
    /// <param name="sender">事件发送者 / Event sender</param>
    /// <param name="e">按键路由事件参数 / Key routed event arguments</param>
    private async void InputBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            await SendMessageAsync();
            e.Handled = true;
        }
    }

    /// <summary>
    /// 发送消息 - 创建用户消息、调用 Agent 并显示响应
    /// Sends a message - creates user message, calls agent and displays response
    /// </summary>
    private async Task SendMessageAsync()
    {
        var input = InputBox.Text?.Trim();
        if (string.IsNullOrEmpty(input) || _agent == null)
            return;

        // 清空输入框 Clear input box
        InputBox.Text = string.Empty;

        // 添加用户消息 Add user message
        AddUserMessage(input);

        // 禁用输入 Disable input
        InputBox.IsEnabled = false;
        SendButton.IsEnabled = false;

        try
        {
            // 创建消息 Create message
            var userMsg = Msg.Builder()
                .Role("user")
                .TextContent(input)
                .Build();

            // 调用 Agent Call agent
            var response = await _agent.CallAsync(userMsg);
            var responseText = response.GetTextContent();

            // 添加 Agent 响应 Add agent response
            AddAssistantMessage(responseText ?? "无响应 No response");
        }
        catch (System.Exception ex)
        {
            AddSystemMessage($"错误 Error: {ex.Message}");
        }
        finally
        {
            // 重新启用输入 Re-enable input
            InputBox.IsEnabled = true;
            SendButton.IsEnabled = true;
            InputBox.Focus(FocusState.Programmatic);
        }
    }

    /// <summary>
    /// 添加用户消息到聊天列表
    /// Adds a user message to the chat list
    /// </summary>
    /// <param name="text">消息文本 / Message text</param>
    private void AddUserMessage(string text)
    {
        _messages.Add(new ChatMessage
        {
            Role = "用户 User",
            Content = text,
            Timestamp = DateTime.Now
        });
        ScrollToBottom();
    }

    /// <summary>
    /// 添加助手响应消息到聊天列表
    /// Adds an assistant response message to the chat list
    /// </summary>
    /// <param name="text">响应文本 / Response text</param>
    private void AddAssistantMessage(string text)
    {
        _messages.Add(new ChatMessage
        {
            Role = "助手 Assistant",
            Content = text,
            Timestamp = DateTime.Now
        });
        ScrollToBottom();
    }

    /// <summary>
    /// 添加系统消息到聊天列表
    /// Adds a system message to the chat list
    /// </summary>
    /// <param name="text">系统消息文本 / System message text</param>
    private void AddSystemMessage(string text)
    {
        _messages.Add(new ChatMessage
        {
            Role = "系统 System",
            Content = text,
            Timestamp = DateTime.Now
        });
        ScrollToBottom();
    }

    /// <summary>
    /// 滚动聊天列表到底部以显示最新消息
    /// Scrolls the chat list to the bottom to show the latest message
    /// </summary>
    private void ScrollToBottom()
    {
        if (ChatListView.Items.Count > 0)
        {
            var lastItem = ChatListView.Items[ChatListView.Items.Count - 1];
            ChatListView.ScrollIntoView(lastItem);
        }
    }
}

/// <summary>
/// 聊天消息数据模型
/// Chat message data model
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 消息角色（用户/助手/系统）
    /// Message role (User/Assistant/System)
    /// </summary>
    public string Role { get; set; } = "";

    /// <summary>
    /// 消息内容
    /// Message content
    /// </summary>
    public string Content { get; set; } = "";

    /// <summary>
    /// 消息时间戳
    /// Message timestamp
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 格式化的时间字符串（HH:mm:ss）
    /// Formatted time string (HH:mm:ss)
    /// </summary>
    public string TimeString => Timestamp.ToString("HH:mm:ss");
}
