# 智能体协议（Agent Protocol）

AgentScope 在 `AgentScope.Core` 和 `AgentScope.Harness` 中提供了多种协议适配器，让 Agent 与外部世界交互。

| 扩展 | 协议 | 解决的问题 |
| --- | --- | --- |
| [A2A](a2a.md) | [Agent-to-Agent](https://a2aproject.github.io/A2A/) | Agent 之间互相调用，组成多 Agent 工作流 |
| [AG-UI](agui.md) | [AG-UI Protocol](https://github.com/ag-ui/ag-ui) | 将 Agent 的事件流标准化输出给前端 UI |
| [Agent Protocol](agent-protocol.md) | [Agent Protocol](https://agentprotocol.ai/) | HTTP 标准接口，供外部系统提交"任务"给 Agent |

## 怎么选

- **想在前端实时消费 Agent 事件流（含推理内容）** → AG-UI
- **让其他业务系统通过 REST 调度 Agent / 托管远程子 agent** → Agent Protocol
- **让多个 Agent 互相调用** → A2A
