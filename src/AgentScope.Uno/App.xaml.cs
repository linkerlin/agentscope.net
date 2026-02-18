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
using AgentScope.Core;
using AgentScope.Core.Configuration;

namespace AgentScope.Uno;

/// <summary>
/// AgentScope.NET Uno Platform 主应用程序
/// Main application entry point for Uno Platform
/// </summary>
public partial class App : Application
{
    private Window? _window;

    public App()
    {
        this.InitializeComponent();
        
        // 加载配置 Load configuration
        ConfigurationManager.Load();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
