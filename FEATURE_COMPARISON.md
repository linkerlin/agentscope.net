# AgentScope.NET vs AgentScope-Java 最新功能对比

> **最后更新**: 2026-08-17 | **.NET 版本**: v2.0.1 (develop/v2.0.1) | 核心模块 22/22 全部完成

## 核心模块对比

| 功能模块 | Java | .NET | 状态 |
|---------|------|------|------|
| Message 系统 | ✅ | ✅ | 完成 |
| Agent (EnhancedReActAgent) | ✅ | ✅ | 完成 |
| Hook 系统 | ✅ | ✅ | 完成 |
| Session 系统 | ✅ | ✅ | 完成 |
| Memory (SQLite) | ✅ | ✅ | 完成 |
| Model (7 种提供商) | ✅ | ✅ | 完成 |
| Formatter (4 种) | ✅ | ✅ | 完成 |
| Tool + ToolGroup | ✅ | ✅ | 完成 |
| Pipeline (7 种节点) | ✅ | ✅ | 完成 |
| Plan (PlanNotebook) | ✅ | ✅ | 完成 |
| RAG | ✅ | ✅ | 完成 |
| **Workflow 引擎** | ❌ | **✅** | .NET 独有 |
| MultiAgent | ✅ | ✅ | 完成 |
| Service 层 | ✅ | ✅ | 完成 |
| Interruption | ✅ | ✅ | 完成 |
| Tracing (含 OTel) | ✅ | ✅ | 完成 |
| Skill 系统 | ✅ | ✅ | 完成 |
| MCP 协议 | ✅ | ✅ | 完成 (3 种客户端) |
| A2A 协议 | ✅ | ✅ | 完成 |
| AgUI 适配 | ✅ | ✅ | 完成 |
| Event 系统 | ✅ | ✅ | 完成 |
| Accumulator | ✅ | ✅ | 完成 |
| TTS | ✅ | ⚠️ | Stub 实现 |
| Harness 运行时 | ✅ | ✅ | 完成 |

## 扩展模块对比 (42 个)

| 类别 | Java | .NET |
|------|------|------|
| 渠道 (DingTalk/Feishu/WeCom/GitHub/GitLab) | ✅ | ✅ |
| 文档 (PDF/Word) | ✅ | ✅ |
| RAG (Bailian/Dify/Haystack/RagFlow) | ✅ | ✅ |
| Sandbox (Docker/K8s/E2B/AgentRun/Daytona) | ✅ | ✅ |
| 调度 (Quartz/XxlJob) | ✅ | ✅ |
| 向量 (Milvus/Qdrant/ES/PgVector) | ✅ | ✅ |
| 存储 (Redis/MySql/OSS/COS/PostgreSql) | ✅ | ✅ |
| 服务发现 (Nacos) | ✅ | ✅ |
| 记忆 (Bailian/Mem0/ReMe) | ✅ | ✅ |
| 其他 (Aistio/Higress/Studio/Training) | ✅ | ✅ |

## 差距项

| 差距 | 优先级 |
|------|--------|
| TTS 真实 Provider (当前 Stub) | P3 |
| OpenAIMultiModalTool 占位实现 | P3 |
| MCP 多客户端命名隔离 | P2 |
| Skill 字段校验深化 | P2 |
| 示例项目不足 (仅 2 个) | P2 |
| 独立文档站点 | P2 |
| CI gate 集成 | P2 |

## 构建状态

- **Core**: ✅ 0 错误
- **Harness**: ✅ 0 错误
- **完整方案**: 🔴 118 错误 / 230 警告
  - 测试 Mock 类 (111) + Uno XAML (7)
- **集成测试**: ✅ 22 通过
