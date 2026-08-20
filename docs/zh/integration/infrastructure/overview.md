# 基础设施 / 中间件

这组扩展把 AgentScope 接到企业基础设施上，让 Agent 像普通微服务一样被治理、调度和发现。

| 扩展 | 中间件 | 主要能力 |
| --- | --- | --- |
| [Higress](higress.md) | [Higress](https://higress.io/) AI 网关 | 通过 MCP 发现并调用网关上的工具 |
| [Nacos](nacos.md) | [Nacos](https://nacos.io/) | A2A AgentCard 注册发现、Prompt 配置中心、Skill 仓库 |
| [Scheduler](scheduler.md) | Quartz.NET / XXL-Job | 按 CRON 或固定速率定时驱动 Agent |

## 整体定位

- **Higress / Nacos** 把"网关、注册中心"作为 AgentScope 的横向基础设施能力。
- **Scheduler** 解决"Agent 由调度器周期性触发"的需求。

各扩展相互独立，按需组合。
