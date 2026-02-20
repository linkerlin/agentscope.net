# AgentScope.NET

一个基于 .NET 平台的 AgentScope 框架实现，用于构建 LLM 驱动的应用程序。这是对 [agentscope-java](https://github.com/agentscope-ai/agentscope-java) 项目的 1:1 移植。

A .NET implementation of the AgentScope framework for building LLM-powered applications. This is a 1:1 port of the [agentscope-java](https://github.com/agentscope-ai/agentscope-java) project.

## 项目状态 Project Status

**完成度 Completion**: ~90% | 21/22 模块完成 | 516 测试 (100% 通过) | 16,000+ 行代码

**当前版本 Version**: v1.0.9

**最新进展 Latest Progress**:
- ✅ 完整的 ReAct Agent 系统（EnhancedReActAgent）
- ✅ Hook 扩展系统（Pre/Post Reasoning/Acting hooks）
- ✅ Session 和 State 管理（线程安全）
- ✅ SQLite 持久化内存
- ✅ 多 LLM 支持（DeepSeek, OpenAI, Azure, Anthropic, DashScope, Gemini, Ollama）
- ✅ Formatter 系统（OpenAI/Anthropic/DashScope/Gemini）
- ✅ Pipeline 编排系统
- ✅ RAG 系统和向量存储
- ✅ Workflow 引擎
- ✅ 多 Agent 编排（AgentGroup, AgentRouter, AgentCoordinator）
- ✅ Tracing 追踪系统
- ✅ Interruption 处理

详细进度请查看：[改进计划.md](改进计划.md) | [CURRENT_STATUS.md](CURRENT_STATUS.md)

## 支持的 LLM 提供商 Supported LLM Providers

| 提供商 Provider | 状态 Status | 说明 Notes |
|-----------------|-------------|------------|
| **DeepSeek** | ✅ 优先支持 | deepseek-chat, deepseek-reasoner |
| **OpenAI** | ✅ 完成 | GPT-3.5/4, 兼容 API |
| **Azure OpenAI** | ✅ 完成 | Azure 部署 |
| **Anthropic** | ✅ 完成 | Claude 系列 |
| **DashScope** | ✅ 完成 | 阿里云通义千问 |
| **Google Gemini** | ✅ 完成 | Gemini Pro/Flash |
| **Ollama** | ✅ 完成 | 本地 LLM (llama2, llama3, mistral, codellama, phi3) |

## 特性 Features

### 核心功能 Core Features ✅
- **EnhancedReActAgent**: 完整的 ReAct 循环（推理-行动-观察）/ Complete ReAct loop
- **Hook 系统**: 可扩展的 Hook 机制 / Extensible hook mechanism
- **Session 管理**: 线程安全的会话管理 / Thread-safe session management
- **消息系统**: 灵活的消息传递，Builder 模式 / Flexible message passing with builder pattern
- **持久化内存**: SQLite + Entity Framework Core / SQLite-based persistent storage
- **工具系统**: 可扩展的工具接口 / Extensible tool interface with schema support
- **配置管理**: .env 文件支持 / Environment variable configuration support
- **Pipeline 编排**: 顺序/并行/条件/循环执行 / Sequential/parallel/conditional/loop execution
- **Plan 管理**: PlanNotebook 任务规划 / Task planning with PlanNotebook
- **RAG 系统**: 向量存储、知识检索 / Vector store, knowledge retrieval
- **Workflow 引擎**: DAG 依赖管理 / DAG dependency management
- **多 Agent 编排**: AgentGroup, AgentRouter, AgentCoordinator
- **Tracing 追踪**: ITracer, Span, TracingManager
- **Interruption 处理**: 可中断/可恢复 Agent / Interruptible/resumable agents

### GUI 应用 GUI Applications ✅
- **TUI 界面**: Terminal.Gui 终端界面 / Terminal user interface
- **Uno Platform GUI**: 跨平台图形界面（基础）/ Cross-platform GUI (basic)

### Java 互操作 Java Interoperability ✅
- ✅ 兼容的 JSON 消息格式 / Compatible JSON message format
- ✅ 共享 SQLite 数据库模式 / Shared SQLite database schema
- ✅ 通用的 .env 配置 / Common .env configuration
- ✅ REST API 兼容性 / REST API compatibility

完整功能清单：[FEATURE_COMPARISON.md](FEATURE_COMPARISON.md)

## 项目结构 Project Structure

```
agentscope.net/
├── src/
│   ├── AgentScope.Core/           # 核心库 Core library (100+ 源文件)
│   │   ├── Agent/                 # Agent 基类和接口
│   │   ├── Hook/                  # Hook 扩展系统
│   │   ├── Session/               # Session 和 State 管理
│   │   ├── Message/               # 消息系统
│   │   ├── Memory/                # 记忆管理（SQLite）
│   │   ├── Model/                 # LLM 模型
│   │   │   ├── Anthropic/         # Claude 模型
│   │   │   ├── DashScope/         # 通义千问模型
│   │   │   ├── DeepSeek/          # DeepSeek 模型
│   │   │   ├── Gemini/            # Google Gemini 模型
│   │   │   ├── Ollama/            # 本地 LLM 模型
│   │   │   └── OpenAI/            # OpenAI 模型
│   │   ├── Formatter/             # LLM 格式化器
│   │   │   ├── OpenAI/            # OpenAI 格式化器
│   │   │   ├── Anthropic/         # Anthropic 格式化器
│   │   │   ├── DashScope/         # DashScope 格式化器
│   │   │   └── Gemini/            # Gemini 格式化器
│   │   ├── Tool/                  # 工具系统
│   │   ├── Pipeline/              # Pipeline 编排
│   │   ├── Plan/                  # Plan 管理
│   │   ├── RAG/                   # RAG 系统
│   │   ├── Workflow/              # Workflow 引擎
│   │   ├── MultiAgent/            # 多 Agent 编排
│   │   ├── Service/               # 服务层
│   │   ├── Interruption/          # 中断处理
│   │   ├── Tracing/               # 追踪系统
│   │   └── Configuration/         # 配置管理
│   └── AgentScope.TUI/            # 终端界面应用
├── examples/
│   └── QuickStart/                # 快速入门示例
├── tests/
│   ├── AgentScope.Core.Tests/     # 单元测试 (500+ tests)
│   └── AgentScope.Integration.Tests/ # 集成测试
├── .env.example                   # 环境变量配置示例
├── AgentScope.slnx                # 解决方案文件
├── README.md                      # 项目说明
├── 改进计划.md                     # 完整实施计划
├── FEATURE_COMPARISON.md          # 功能对比分析
├── CURRENT_STATUS.md              # 当前状态
├── INTEROPERABILITY.md            # Java 互操作性文档
└── CONTRIBUTING.md                # 贡献指南
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
# DEEPSEEK_API_KEY=your_key_here
# OPENAI_API_KEY=your_key_here
# ANTHROPIC_API_KEY=your_key_here
# DASHSCOPE_API_KEY=your_key_here
# GEMINI_API_KEY=your_key_here
# DATABASE_PATH=agentscope.db
```

### 运行测试 Run Tests

```bash
# 运行所有测试 Run all tests (516 tests, 100% passing)
dotnet test

# 运行单元测试 Run unit tests only
dotnet test tests/AgentScope.Core.Tests/

# 运行集成测试 Run integration tests only
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
- `EnhancedReActAgent`: 增强版 ReAct 实现 / Enhanced ReAct implementation
- `InterruptibleAgentBase`: 可中断 Agent 基类 / Interruptible agent base

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
- `ContentBlock`: 内容块 / Content block

### Memory
- `IMemory`: 内存接口 / Memory interface
- `MemoryBase`: 内存基础实现 / Basic memory implementation
- `SqliteMemory`: SQLite 持久化内存 / SQLite persistent memory

### Model
- `IModel`: 模型接口 / Model interface
- `ModelBase`: 模型基类 / Model base class
- `MockModel`: 模拟模型 (用于测试) / Mock model for testing
- `DeepSeekModel`, `OpenAIModel`, `AnthropicModel`, `GeminiModel`, `OllamaModel`, `DashScopeModel`

### Tool
- `ITool`: 工具接口 / Tool interface
- `ToolBase`: 工具基类 / Tool base class
- `WebSearchTool`: 网页搜索工具 / Web search tool
- `CodeExecutionTool`: 代码执行工具 / Code execution tool

### Pipeline
- `IPipelineNode`: Pipeline 节点接口 / Pipeline node interface
- `Pipeline`: Pipeline 类 / Pipeline class
- `Nodes`: Pipeline 节点（顺序/并行/条件/循环）/ Pipeline nodes

### RAG
- `IKnowledge`: 知识接口 / Knowledge interface
- `VectorStore`: 向量存储 / Vector store
- `RAGHook`: RAG Hook / RAG hook
- `KnowledgeRetrievalTools`: 知识检索工具 / Knowledge retrieval tools

### Workflow
- `IWorkflow`: Workflow 接口 / Workflow interface
- `WorkflowEngine`: Workflow 引擎 / Workflow engine

### Multi-Agent
- `AgentGroup`: Agent 组 / Agent group
- `AgentRouter`: Agent 路由器 / Agent router
- `AgentCoordinator`: Agent 协调器 / Agent coordinator

### Tracing
- `ITracer`: Tracer 接口 / Tracer interface
- `TraceSpan`: Span 实现 / Span implementation
- `TracingManager`: 追踪管理器 / Tracing manager

## 技术栈 Tech Stack

| 类别 Category | 技术 Technology |
|---------------|-----------------|
| **语言 Language** | C# (.NET 9.0) |
| **ORM** | Entity Framework Core 9.0.1 |
| **数据库 Database** | SQLite |
| **响应式编程 Reactive** | System.Reactive 6.1.0 |
| **终端界面 Terminal UI** | Terminal.Gui 1.17.1 |
| **跨平台 GUI** | Uno Platform 5.6.4 |
| **配置管理 Config** | DotNetEnv 3.1.1 |
| **测试框架 Testing** | xUnit 2.9.3 |
| **JSON序列化 JSON** | System.Text.Json |
| **许可证 License** | Apache License 2.0 |

## 测试 Testing

项目包含 516 测试用例，确保代码质量 / The project includes 516 test cases to ensure code quality:

- **单元测试 Unit Tests**: 测试单个组件 / Test individual components
  - Message system
  - Agent infrastructure
  - Memory management
  - Model system
  - Tool system
  - Configuration
  - Session management
  - Pipeline
  - RAG
  - Workflow
  - Multi-Agent
  - Tracing

- **集成测试 Integration Tests**: 测试组件间交互 / Test component interactions
  - Agent-Memory workflows
  - Multi-component integration
  - End-to-end scenarios

**测试通过率 Test Pass Rate**: 100% ✅

```bash
# 运行所有测试并显示详细信息 Run all tests with details
dotnet test --logger "console;verbosity=detailed"
```

## 贡献 Contributing

欢迎贡献！请查看 [CONTRIBUTING.md](CONTRIBUTING.md) 了解详情。

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for details.

### 项目文档 Project Documentation

- **[改进计划.md](改进计划.md)** - 完整实施计划 / Complete implementation plan
- **[FEATURE_COMPARISON.md](FEATURE_COMPARISON.md)** - Java vs .NET 功能对比 / Feature comparison
- **[CURRENT_STATUS.md](CURRENT_STATUS.md)** - 当前状态 / Current status
- **[INTEROPERABILITY.md](INTEROPERABILITY.md)** - Java 互操作性 / Java interoperability
- **[CONTRIBUTING.md](CONTRIBUTING.md)** - 贡献指南 / Contributing guide

### 如何参与 How to Contribute

1. Fork 本仓库 / Fork this repository
2. 创建特性分支 / Create a feature branch
3. 遵循现有代码风格和测试标准 / Follow existing code style and testing standards
4. 提交 Pull Request 并包含测试和文档 / Submit Pull Request with tests and documentation

## 许可证 License

Apache License 2.0

## 致谢 Acknowledgments

本项目是 [agentscope-java](https://github.com/agentscope-ai/agentscope-java) 的 .NET 移植版本。感谢原项目团队的出色工作。

This project is a .NET port of [agentscope-java](https://github.com/agentscope-ai/agentscope-java). Thanks to the original team for their excellent work.
