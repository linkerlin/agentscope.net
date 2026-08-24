# Infrastructure / Middleware

These extensions plug AgentScope into enterprise infrastructure so an Agent can be governed, discovered, and scheduled like any other microservice.

| Extension | Middleware | Capability |
| --- | --- | --- |
| [Higress](higress.md) | [Higress](https://higress.io/) AI gateway | Discover and invoke tools published as MCP on the gateway |
| [Nacos](nacos.md) | [Nacos](https://nacos.io/) | A2A AgentCard registry/discovery, prompt config center, skill repository |
| [Scheduler](scheduler.md) | Quartz.NET / XXL-Job | Run Agents on a CRON schedule or fixed rate |

## Where this fits

- **Higress / Nacos** make "gateway, registry" horizontal capabilities of AgentScope.
- **Scheduler** addresses the "Agent triggered by a scheduler, not a human" use case.

These extensions are independent and can be combined freely.
