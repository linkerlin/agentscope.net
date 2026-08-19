# AgentScope Studio

`AgentScope.Extensions.Studio` 把 Agent 接入 [AgentScope Studio](https://github.com/agentscope-ai/agentscope-studio)：每次 Agent 调用都会被推送到 Studio，用作可视化调试、链路回放、Human-in-the-Loop 输入。

## 何时使用

- 开发期想在 Studio 里看到 Agent 的事件流、推理过程、工具调用。
- 需要在 Studio 里发起 `RequestUserInput`，让真人介入答题。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Studio" Version="$(AgentScopeVersion)" />
```

## 快速上手

```csharp
using AgentScope.Core.Studio;

// 1) 初始化 Studio 连接（HTTP + WebSocket）
StudioManager.Init()
    .StudioUrl("http://localhost:8000")
    .Project("MyProject")
    .RunName("experiment_001")
    .Initialize()
    .Wait();

// 2) 把 StudioMessageHook 挂到 Agent 上，自动把消息推到 Studio
ReActAgent agent = ReActAgent.Builder()
    .Name("Assistant")
    .Model(model)
    .Hook(new StudioMessageHook(StudioManager.GetClient()))
    .Build();

// 3) 正常调用 Agent，Studio 上会同步看到对话
agent.Call(msg).Wait();
```

## Studio 提供的能力

- **消息推送**：每条 user / assistant / tool 消息都被同步到 Studio。
- **链路追踪**：Studio 内部会按 `RunName` 把整次运行编排成一棵 trace 树。
- **Human-in-the-Loop**：通过 `StudioUserAgent` 或 `RequestUserInput`，让 Studio UI 弹出输入框等待真人填写后再继续。

## API 概览

| 类 | 用途 |
| --- | --- |
| `StudioManager` | 单例式入口，初始化和获取 client |
| `StudioConfig` | URL / project / runName 等配置 |
| `StudioClient` | HTTP 客户端，推送事件、消息、注册 run |
| `StudioWebSocketClient` | WebSocket 客户端，接收 Studio 侧的指令（如 user input） |
| `StudioMessageHook` | 注入到 `ReActAgent` 的 `Hook`，自动推送 `Msg` |
| `StudioUserAgent` | "真人扮演的 Agent"，调用时阻塞等待 Studio 输入 |

## 何时关闭

生产部署一般不挂这个 Hook（避免每次调用都向 Studio 写一份）。建议通过配置控制：

```csharp
// 仅在存在配置时启用
if (configuration.GetSection("AgentScope:Studio").Exists())
{
    StudioManager.Init()
        .StudioUrl(url)
        .Project(project)
        .Initialize()
        .Wait();
    services.AddSingleton(new StudioMessageHook(StudioManager.GetClient()));
}
```
