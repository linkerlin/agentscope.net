# Agent Protocols

AgentScope provides several protocol adapters in `AgentScope.Core` and `AgentScope.Harness` for Agents to interact with the outside world.

| Extension | Protocol | Problem it solves |
| --- | --- | --- |
| [A2A](a2a.md) | [Agent-to-Agent](https://a2aproject.github.io/A2A/) | Agents calling each other, composing multi-agent workflows |
| [AG-UI](agui.md) | [AG-UI Protocol](https://github.com/ag-ui/ag-ui) | Standardized event stream for front-end UIs |
| [Agent Protocol](agent-protocol.md) | [Agent Protocol](https://agentprotocol.ai/) | HTTP-based task submission for external systems |

## Choosing one

- **Stream Agent events to a front-end UI (incl. reasoning)** → AG-UI
- **Let backend systems schedule the Agent over REST** → Agent Protocol
- **Let multiple Agents call each other** → A2A
