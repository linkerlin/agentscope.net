## 📋 AgentScope.NET 工作需求详述

### 一、现状分析

**好消息**：AgentScope.NET 框架已经存在且完成度约 **92%** (v1.1.0)！

- 仓库地址：https://github.com/linkerlin/agentscope.net
- 完成度：22/22 模块，537 测试 (100% 通过)，16,500+ 行代码
- 核心功能全部完成，包括 EnhancedReActAgent、Hook 系统、Tool 系统、ModelFactory、ToolFactory 等

**已完成**：
- ✅ ModelFactory 统一模型工厂（7 种提供商）
- ✅ ToolFactory 统一工具工厂（4 种内置工具）
- ✅ 537 个单元测试

**问题**：MyClaw.NET 项目中的引用路径 `..\..\..\agentscope.net\src\AgentScope.Core` 不存在，需要正确集成。

---

### 二、需要完成的工作

#### 🔷 任务 1：获取并集成 AgentScope.NET

**方式 A - 克隆源码（推荐用于开发）**：
```bash
# 在 myclaw.net 同级目录克隆
git clone https://github.com/linkerlin/agentscope.net.git
```

**方式 B - 使用 NuGet 包（发布后）**：
```xml
<PackageReference Include="AgentScope.Core" Version="1.0.9" />
```

**当前项目引用需要调整**：
- 修改 [MyClaw.Agent.csproj](file:///c:/GitHub/myclaw.net/src/MyClaw.Agent/MyClaw.Agent.csproj#L9) 中的项目引用

---

#### 🔷 任务 2：完善 MyClawAgent 实现

**当前代码已具备基础结构** ([MyClawAgent.cs](file:///c:/GitHub/myclaw.net/src/MyClaw.Agent/MyClawAgent.cs))：

| 当前状态 | 需要完善 |
|---------|---------|
| ✅ 构造函数已定义 | 需验证 EnhancedReActAgent.Builder() API |
| ✅ ChatAsync 方法 | 需完善错误处理和流式响应 |
| ✅ BuildSystemPrompt() | 需支持更多上下文（HEARTBEAT.md 等） |
| ⚠️ SkillTool 实现 | 需要重新实现 Tool 接口 |

**需要实现的功能**：

1. **单次消息模式** (`myclaw agent -m "Hello"`)
   - 接收单条用户消息
   - 返回 Agent 响应
   - 退出

2. **REPL 交互模式** (`myclaw agent`)
   - 循环读取用户输入
   - 保持会话上下文
   - 支持退出命令 (`/exit`, `/quit`)

3. **系统提示词构建**
   - 加载 [AGENTS.md](file:///c:/GitHub/myclaw.net/workspace/AGENTS.md)
   - 加载 [SOUL.md](file:///c:/GitHub/myclaw.net/workspace/SOUL.md)
   - 加载 Memory 上下文
   - 加载 HEARTBEAT.md（心跳任务说明）

---

#### 🔷 任务 3：重构 SkillTool

**当前实现问题** ([SkillTool.cs](file:///c:/GitHub/myclaw.net/src/MyClaw.Agent/SkillTool.cs))：

```csharp
// 当前：Skill 作为提示词返回（不正确）
public override Task<ToolResult> ExecuteAsync(Dictionary<string, object> parameters)
{
    var prompt = _skill.GetSystemPrompt();
    return Task.FromResult(ToolResult.Ok(prompt));
}
```

**应该实现的逻辑**：
- 解析用户意图匹配 Skill 关键词
- 将 Skill 能力转换为 Agent 可调用的 Tool
- 执行 Skill 逻辑并返回结构化结果

---

#### 🔷 任务 4：完善 ModelFactory

**当前支持的模型** ([ModelFactory.cs](file:///c:/GitHub/myclaw.net/src/MyClaw.Agent/ModelFactory.cs))：
- ✅ Anthropic (Claude)
- ✅ OpenAI (GPT-4)
- ✅ DeepSeek

**AgentScope.NET 还支持**：
- Azure OpenAI
- Google Gemini
- 阿里云 DashScope (通义千问)
- Ollama (本地模型)

需要根据配置扩展支持。

---

#### 🔷 任务 5：CLI 命令完善

**当前 AgentCommand 状态**：需要检查 [AgentCommand.cs](file:///c:/GitHub/myclaw.net/src/MyClaw.CLI/Commands/AgentCommand.cs)

需要实现的 CLI 参数：
```bash
myclaw agent                         # REPL 模式
myclaw agent -m "Hello"              # 单次消息模式
myclaw agent -m "Hello" --model gpt-4o  # 指定模型
myclaw agent --repl                  # 显式 REPL 模式
```

---

#### 🔷 任务 6：与 Gateway 集成

在 Gateway 模式下，Agent 需要作为消息处理器：

```
Telegram/Feishu/WeCom/WebUI → Channel → MessageBus → MyClawAgent → Response
```

需要实现：
- `IMessageHandler` 接口
- 消息路由到 Agent 的逻辑
- 会话管理（session_id）

---

### 三、最终交付物清单

| 交付物 | 文件路径 | 描述 |
|-------|---------|------|
| 1. 可运行的 Agent CLI | `src/MyClaw.CLI/Commands/AgentCommand.cs` | 支持单次和 REPL 模式 |
| 2. MyClawAgent 核心类 | `src/MyClaw.Agent/MyClawAgent.cs` | 继承 EnhancedReActAgent |
| 3. Skill 工具适配器 | `src/MyClaw.Agent/SkillTool.cs` | 将 Skill 转为 ITool |
| 4. 模型工厂 | `src/MyClaw.Agent/ModelFactory.cs` | 支持多 LLM 提供商 |
| 5. REPL 循环组件 | `src/MyClaw.Agent/ReplLoop.cs` | 交互式命令行界面 |
| 6. Gateway 集成 | `src/MyClaw.Gateway/` | Agent 作为消息处理器 |
| 7. 单元测试 | `tests/MyClaw.Agent.Tests/` | 覆盖率 ≥ 80% |

---

### 四、依赖关系图

```
MyClaw.CLI (入口)
    │
    ├── MyClaw.Agent (本任务)
    │   ├── EnhancedReActAgent (AgentScope.NET) ✅ 已具备
    │   ├── IModel (OpenAI/Anthropic/DeepSeek) ✅ 已具备
    │   ├── ITool/ToolBase (AgentScope.NET) ✅ 已具备
    │   └── SkillManager (已有)
    │
    ├── MyClaw.Gateway (已有框架)
    │   └── 需要集成 MyClawAgent
    │
    ├── MyClaw.Skills (已有)
    └── MyClaw.Memory (已有)
```

---

### 五、开发建议

1. **先克隆 AgentScope.NET** 到本地并验证构建成功
2. **先让 Agent 单机模式工作**，再集成到 Gateway
3. **参考原版 myclaw (Go)** 的行为：https://github.com/stellarlinkco/myclaw
4. **测试优先级**：单次消息 → REPL 模式 → Gateway 集成

---

