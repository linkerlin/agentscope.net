# AgentScope.NET

一个基于 .NET 平台的 AgentScope 框架实现，用于构建 LLM 驱动的应用程序。这是对 [agentscope-java](https://github.com/agentscope-ai/agentscope-java) 项目的 1:1 移植。

A .NET implementation of the AgentScope framework for building LLM-powered applications. This is a 1:1 port of the [agentscope-java](https://github.com/agentscope-ai/agentscope-java) project.

## 项目状态 Project Status

**完成度 Completion**: 23/54 功能点 (42.6%) | 79+ 测试 (100% 通过) | 5,750+ 行代码

**最新进展 Latest Progress**:
- ✅ 核心 Agent 系统（EnhancedReActAgent with ReAct loop）
- ✅ Hook 扩展系统（Pre/Post Reasoning/Acting hooks）
- ✅ Session 和 State 管理（线程安全）
- ✅ SQLite 持久化内存
- ✅ OpenAI Formatter DTO 模型（Phase 1）
- ⏳ Formatter 转换器和解析器（进行中）

详细进度请查看：[改进计划.md](改进计划.md) | [CURRENT_STATUS.md](CURRENT_STATUS.md)

## 特性 Features

### 已实现 Implemented ✅
- **EnhancedReActAgent**: 完整的 ReAct 循环（推理-行动-观察）/ Complete ReAct loop (Reasoning-Acting-Observation)
- **Hook 系统**: 可扩展的 Hook 机制 / Extensible hook mechanism for pre/post processing
- **Session 管理**: 线程安全的会话管理 / Thread-safe session management
- **消息系统**: 灵活的消息传递 / Flexible message passing with builder pattern
- **持久化内存**: SQLite + Entity Framework Core / SQLite-based persistent storage
- **工具系统**: 可扩展的工具接口 / Extensible tool interface with schema support
- **配置管理**: .env 文件支持 / Environment variable configuration support
- **全面测试**: 79+ 测试全部通过 / 79+ tests, 100% passing
- **TUI 界面**: Terminal.Gui 终端界面 / Terminal user interface
- **Uno Platform GUI**: 跨平台图形界面（基础）/ Cross-platform GUI (basic)
- **Java 互操作**: 兼容的消息格式 / Compatible message format

### 开发中 In Progress ⏳
- **Formatter 系统**: OpenAI/Anthropic/DashScope 格式化器 / LLM provider formatters
- **真实 LLM 集成**: OpenAI/Azure OpenAI 模型 / Real LLM model integration

### 计划中 Planned 📋
- **Pipeline 编排**: 顺序/并行/条件执行 / Sequential/parallel/conditional execution
- **Plan 管理**: PlanNotebook 任务规划 / Task planning with PlanNotebook
- **RAG 系统**: 知识检索增强生成 / Knowledge retrieval augmented generation
- **Tracing**: OpenTelemetry 可观测性 / Observability with OpenTelemetry
- **MCP/A2A 协议**: 多 Agent 通信 / Multi-agent communication protocols

完整功能清单：[FEATURE_COMPARISON.md](FEATURE_COMPARISON.md)

## 项目结构 Project Structure

```
agentscope.net/
├── src/
│   ├── AgentScope.Core/           # 核心库 Core library
│   │   ├── Agent/                 # Agent 基类和接口
│   │   ├── Hook/                  # Hook 扩展系统 ✨
│   │   ├── Session/               # Session 和 State 管理 ✨
│   │   ├── Message/               # 消息系统
│   │   ├── Memory/                # 记忆管理（SQLite）
│   │   ├── Model/                 # LLM 模型接口
│   │   ├── Tool/                  # 工具系统
│   │   ├── Formatter/             # LLM 格式化器 ✨
│   │   │   ├── IFormatter.cs      # 格式化器接口
│   │   │   └── OpenAI/            # OpenAI 格式化器
│   │   │       └── Dto/           # DTO 模型（完成）
│   │   ├── Configuration/         # 配置管理（.env）
│   │   ├── Exception/             # 异常定义
│   │   └── ...                    # 其他模块
│   ├── AgentScope.TUI/            # 终端界面应用
│   └── AgentScope.Uno/            # Uno Platform GUI ✨
├── examples/                      # 示例代码
│   └── QuickStart/               # 快速入门示例
├── tests/                         # 测试（79+ tests）
│   ├── AgentScope.Core.Tests/            # 单元测试 (50)
│   └── AgentScope.Integration.Tests/     # 集成测试 (7)
├── .env.example                   # 环境变量配置示例
├── 改进计划.md                     # 完整实施计划 ⭐⭐⭐
├── FEATURE_COMPARISON.md          # 功能对比分析
├── CURRENT_STATUS.md              # 当前状态快照
├── 实施总结报告.md                 # 项目总结报告
└── 工作总结与继续实施指南.md        # 实施指南
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
# 运行所有测试 Run all tests (79+ tests, 100% passing)
dotnet test

# 运行单元测试 Run unit tests only (50 tests)
dotnet test tests/AgentScope.Core.Tests/

# 运行集成测试 Run integration tests only (7 tests)
dotnet test tests/AgentScope.Integration.Tests/

# 详细输出 Verbose output
dotnet test --logger "console;verbosity=detailed"
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
- `EnhancedReActAgent`: 增强版 ReAct 实现 / Enhanced ReAct implementation with tool execution

### Hook System

- `IHook`: Hook 接口 / Hook interface
- `HookManager`: Hook 管理器 / Hook manager
- `PreReasoningEvent`, `PostReasoningEvent`: 推理钩子 / Reasoning hooks
- `PreActingEvent`, `PostActingEvent`: 行动钩子 / Acting hooks

### Session

- `Session`: 会话类 / Session class
- `SessionManager`: 会话管理器 / Session manager (thread-safe)

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

### 已完成 Completed ✅
- [x] 核心消息系统 / Core message system
- [x] Agent 基础架构 / Agent infrastructure
- [x] EnhancedReActAgent with ReAct loop
- [x] Hook 扩展系统 / Hook system
- [x] Session 和 State 管理 / Session and state management
- [x] 持久化内存（SQLite + EF Core）/ Persistent memory
- [x] 基础模型接口 / Basic model interface
- [x] 工具系统和示例 / Tool system with examples
- [x] TUI 应用 / TUI application
- [x] Uno Platform GUI（基础）/ Cross-platform GUI (basic)
- [x] .env 配置支持 / .env configuration support
- [x] 全面的单元测试（50 tests）/ Comprehensive unit tests
- [x] 集成测试（7 tests）/ Integration tests
- [x] Java 互操作性文档 / Java interoperability documentation
- [x] OpenAI Formatter DTO 模型 / OpenAI formatter DTOs

### 进行中 In Progress ⏳
- [ ] OpenAI Formatter 完整实现 / Complete OpenAI formatter
- [ ] 真实 LLM 模型集成 / Real LLM model integration

### 计划中 Planned 📋
- [ ] Anthropic/DashScope Formatter
- [ ] Pipeline 编排系统 / Pipeline orchestration
- [ ] Plan 管理（PlanNotebook）/ Plan management
- [ ] RAG 支持 / RAG support
- [ ] Tracing 和 Observability / Tracing and observability
- [ ] Interruption 处理 / Interruption handling
- [ ] MCP 协议支持 / MCP protocol support
- [ ] A2A 协议支持 / A2A protocol support
- [ ] Agent 变体（Callable, Observable等）/ Agent variants

完整路线图请参考：[改进计划.md](改进计划.md)

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

项目包含 79+ 测试用例，确保代码质量 / The project includes 79+ test cases to ensure code quality:

- **单元测试 Unit Tests (50)**: 测试单个组件 / Test individual components
  - Message system (13 tests)
  - Agent infrastructure (5 tests)
  - Memory management (11 tests)
  - Model system (5 tests)
  - Tool system (7 tests)
  - Configuration (6 tests)
  - Session management (25 tests)

- **集成测试 Integration Tests (7)**: 测试组件间交互 / Test component interactions
  - Agent-Memory workflows (3 tests)
  - Multi-component integration (2 tests)
  - End-to-end scenarios (2 tests)

**测试通过率 Test Pass Rate**: 100% ✅

```bash
# 运行所有测试并显示详细信息 Run all tests with details
dotnet test --logger "console;verbosity=detailed"

# 检查测试覆盖率 Check test coverage
dotnet test /p:CollectCoverage=true
```

## 贡献 Contributing

欢迎贡献！请查看 [CONTRIBUTING.md](CONTRIBUTING.md) 了解详情。

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

### 项目文档 Project Documentation

- **[改进计划.md](改进计划.md)** - 完整的54个功能点实施计划 / Complete implementation plan for 54 features ⭐⭐⭐
- **[FEATURE_COMPARISON.md](FEATURE_COMPARISON.md)** - Java vs .NET 功能对比 / Feature comparison
- **[CURRENT_STATUS.md](CURRENT_STATUS.md)** - 当前状态快照 / Current status snapshot
- **[实施总结报告.md](实施总结报告.md)** - 项目总结报告 / Implementation summary report
- **[工作总结与继续实施指南.md](工作总结与继续实施指南.md)** - 继续实施指南 / Continuation guide
- **[INTEROPERABILITY.md](INTEROPERABILITY.md)** - Java 互操作性 / Java interoperability
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - 贡献指南 / Contributing guide

### 如何参与 How to Contribute

1. 阅读 [改进计划.md](改进计划.md) 了解未完成的功能
2. 选择一个功能点或 Step 开始实施
3. 遵循现有代码风格和测试标准
4. 提交 Pull Request 并包含测试和文档

## 许可证 License

Apache License 2.0

## 致谢 Acknowledgments

本项目是 [agentscope-java](https://github.com/agentscope-ai/agentscope-java) 的 .NET 移植版本。感谢原项目团队的出色工作。

This project is a .NET port of [agentscope-java](https://github.com/agentscope-ai/agentscope-java). Thanks to the original team for their excellent work.
