# AgentScope.NET

一个基于 .NET 平台的 AgentScope 框架实现，用于构建 LLM 驱动的应用程序。这是对 [agentscope-java](https://github.com/agentscope-ai/agentscope-java) 项目的 1:1 移植。

A .NET implementation of the AgentScope framework for building LLM-powered applications. This is a 1:1 port of the [agentscope-java](https://github.com/agentscope-ai/agentscope-java) project.

## 特性 Features

- **ReActAgent**: 结合推理和行动的 Agent 实现 / Agent implementation combining reasoning and acting
- **消息系统**: 灵活的消息传递机制 / Flexible message passing mechanism  
- **持久化内存**: 基于 SQLite 的持久化存储 / SQLite-based persistent storage
- **工具系统**: 可扩展的工具接口 / Extensible tool interface
- **TUI 界面**: 基于 Terminal.Gui 的终端用户界面 / Terminal user interface with Terminal.Gui
- **响应式编程**: 使用 System.Reactive 实现非阻塞执行 / Non-blocking execution with System.Reactive
- **.env 配置**: 支持环境变量配置 (LLM API密钥等) / Environment variable configuration support
- **全面测试**: 50+ 单元测试和集成测试 / 50+ unit and integration tests
- **Java 互操作**: 与 agentscope-java 兼容的消息格式 / Compatible with agentscope-java message format

## 项目结构 Project Structure

```
agentscope.net/
├── src/
│   ├── AgentScope.Core/      # 核心库 Core library
│   │   ├── Agent/            # Agent 基类和接口
│   │   ├── Message/          # 消息系统
│   │   ├── Memory/           # 记忆管理
│   │   ├── Model/            # LLM 模型接口
│   │   ├── Tool/             # 工具系统
│   │   ├── Configuration/    # 配置管理
│   │   ├── Exception/        # 异常定义
│   │   └── ...              # 其他模块
│   ├── AgentScope.TUI/       # 终端界面应用
│   └── AgentScope.Gui/       # GUI 应用 (开发中)
├── examples/                 # 示例代码
│   └── QuickStart/          # 快速入门示例
├── tests/                    # 测试
│   ├── AgentScope.Core.Tests/           # 单元测试 (43 tests)
│   └── AgentScope.Integration.Tests/    # 集成测试 (7 tests)
└── .env.example             # 环境变量配置示例
```

## 快速开始 Quick Start

### 前置要求 Requirements

- .NET 9.0 或更高版本 / .NET 9.0 or higher
- SQLite

### 构建项目 Build

```bash
dotnet build
```

### 配置环境变量 Configure Environment Variables

```bash
# 复制配置文件示例 Copy example configuration
cp .env.example .env

# 编辑 .env 文件并填入你的 API 密钥 Edit .env and add your API keys
# OPENAI_API_KEY=your_key_here
# AZURE_OPENAI_API_KEY=your_key_here
# DATABASE_PATH=agentscope.db
```

### 运行测试 Run Tests

```bash
# 运行所有测试 Run all tests (50 tests)
dotnet test

# 运行单元测试 Run unit tests only (43 tests)
dotnet test tests/AgentScope.Core.Tests/

# 运行集成测试 Run integration tests only (7 tests)
dotnet test tests/AgentScope.Integration.Tests/
```

### 运行 TUI 应用 Run TUI Application

```bash
cd src/AgentScope.TUI
dotnet run
```

### 使用示例 Usage Example

```csharp
using AgentScope.Core;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using AgentScope.Core.Memory;

// 创建模型 Create model
var model = MockModel.Builder()
    .ModelName("mock-model")
    .Build();

// 创建内存 Create memory
var memory = new SqliteMemory("agentscope.db");

// 创建 Agent Create agent
var agent = ReActAgent.Builder()
    .Name("Assistant")
    .SysPrompt("You are a helpful AI assistant.")
    .Model(model)
    .Memory(memory)
    .Build();

// 发送消息 Send message
var userMsg = Msg.Builder()
    .Role("user")
    .TextContent("Hello!")
    .Build();

var response = await agent.CallAsync(userMsg);
Console.WriteLine(response.GetTextContent());
```

## 核心组件 Core Components

### Agent

- `IAgent`: Agent 接口 / Agent interface
- `AgentBase`: Agent 基类 / Agent base class
- `ReActAgent`: ReAct 模式实现 / ReAct pattern implementation

### Message

- `Msg`: 消息类 / Message class
- `MsgBuilder`: 消息构建器 / Message builder

### Memory

- `IMemory`: 内存接口 / Memory interface
- `MemoryBase`: 内存基础实现 / Basic memory implementation
- `SqliteMemory`: SQLite 持久化内存 / SQLite persistent memory

### Model

- `IModel`: 模型接口 / Model interface
- `ModelBase`: 模型基类 / Model base class
- `MockModel`: 模拟模型 (用于测试) / Mock model for testing

### Tool

- `ITool`: 工具接口 / Tool interface
- `ToolBase`: 工具基类 / Tool base class
- `ToolResult`: 工具执行结果 / Tool execution result

## 技术栈 Tech Stack

- **.NET 9.0**: 核心运行时 / Core runtime
- **Entity Framework Core**: ORM 框架 / ORM framework
- **SQLite**: 数据库 / Database
- **System.Reactive**: 响应式编程 / Reactive programming
- **Terminal.Gui**: 终端界面 / Terminal UI
- **Newtonsoft.Json**: JSON 序列化 / JSON serialization

## 开发路线图 Roadmap

- [x] 核心消息系统 / Core message system
- [x] Agent 基础架构 / Agent infrastructure
- [x] 持久化内存 / Persistent memory
- [x] 基础模型接口 / Basic model interface
- [x] TUI 应用 / TUI application
- [x] .env 配置支持 / .env configuration support
- [x] 全面的单元测试 (43 tests) / Comprehensive unit tests
- [x] 集成测试 (7 tests) / Integration tests
- [x] Java 互操作性文档 / Java interoperability documentation
- [ ] 完整 ReAct 循环 / Complete ReAct loop
- [ ] 工具调用支持 / Tool calling support
- [ ] Hook 系统 / Hook system
- [ ] 结构化输出 / Structured output
- [ ] RAG 支持 / RAG support
- [ ] MCP 协议支持 / MCP protocol support
- [ ] A2A 协议支持 / A2A protocol support
- [ ] 更多 LLM 模型支持 / More LLM model support
- [ ] Uno Platform GUI / Cross-platform GUI

## Java 互操作性 Java Interoperability

AgentScope.NET 与 agentscope-java 完全兼容。详见 [INTEROPERABILITY.md](INTEROPERABILITY.md)。

AgentScope.NET is fully compatible with agentscope-java. See [INTEROPERABILITY.md](INTEROPERABILITY.md) for details.

**主要特性 Key Features:**
- ✅ 兼容的 JSON 消息格式 / Compatible JSON message format
- ✅ 共享 SQLite 数据库模式 / Shared SQLite database schema
- ✅ 通用的 .env 配置 / Common .env configuration
- ✅ REST API 兼容性 / REST API compatibility
- ✅ 消息队列支持 / Message queue support

## 测试 Testing

项目包含 50+ 测试用例，确保代码质量 / The project includes 50+ test cases to ensure code quality:

- **单元测试 Unit Tests (43)**: 测试单个组件 / Test individual components
  - Message system (13 tests)
  - Agent infrastructure (5 tests)
  - Memory management (11 tests)
  - Model system (5 tests)
  - Tool system (7 tests)
  - Configuration (6 tests)

- **集成测试 Integration Tests (7)**: 测试组件间交互 / Test component interactions
  - Agent-Memory workflows
  - Multi-agent communication
  - End-to-end scenarios

```bash
# 运行所有测试并显示详细信息 Run all tests with details
dotnet test --logger "console;verbosity=detailed"

# 检查测试覆盖率 Check test coverage
dotnet test /p:CollectCoverage=true
```

## 贡献 Contributing

欢迎贡献！请查看 CONTRIBUTING.md 了解详情。

Contributions are welcome! Please see CONTRIBUTING.md for details.

## 许可证 License

Apache License 2.0

## 致谢 Acknowledgments

本项目是 [agentscope-java](https://github.com/agentscope-ai/agentscope-java) 的 .NET 移植版本。感谢原项目团队的出色工作。

This project is a .NET port of [agentscope-java](https://github.com/agentscope-ai/agentscope-java). Thanks to the original team for their excellent work.
