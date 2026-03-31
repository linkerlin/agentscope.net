# AgentScope.NET TODO（校准版）

## 当前基线

- 校准日期：2026-03-31
- 当前实测：dotnet test AgentScope.slnx 通过 678 项测试，0 失败，0 个 warning
- 快速验证路径：pwsh ./scripts/test-fast.ps1 通过 663 项测试，0 失败
- 外部依赖 smoke 路径：pwsh ./scripts/test-external.ps1 通过 4 项测试，0 失败
- 外部依赖完整路径：pwsh ./scripts/test-external.ps1 -Full 通过 15 项测试，0 失败
- 当前问题：文档把多项已有能力误判为缺失，实际优先级需要按成熟度和接入度重排
- 当前生态：示例已扩展为 QuickStart 与 WebSocketEcho，统一文档入口仍不足

## 已确认存在的基础能力

以下能力已在仓库中存在基础实现，不应再按“从零建设”排期：

- [x] Event / EventType
- [x] IStreamableAgent / StreamOptions / AgentStreamAdapter
- [x] Accumulator
- [x] Hook 基础事件与 HookManager
- [x] State 协议与状态对象
- [x] ToolGroup / ToolGroupManager
- [x] 文件工具与命令工具
- [x] SubAgentTool
- [x] MCP 抽象与 McpTool
- [x] TTS 接口与 Stub 实现
- [x] WebSocket 传输底座
- [x] OpenAI 多模态工具骨架
- [x] WebSearchTool 与 provider fallback

## P0：先修正基线，再打通主链路

- [x] 将 capability-scan 从“文件存在”升级为“文件存在 / 已测试 / 已接入 / 真实 provider”四级评估
- [x] 修复 GeminiFormatterTests 中的 2 个可空 warning
- [x] 修复 LlmSystemTests 中的 1 个可空 warning
- [x] 为 AgentStreamAdapter 与 streaming 主路径补单测或集成测试
- [x] 打通真实 chunk 事件流，而不是仅在 CallAsync 结束后合成开始/结束事件
- [x] 在 EnhancedReActAgent 中接入 ReasoningChunk、ActingChunk、Error 事件
- [x] 补 Summary 事件链路，打通 SummaryStart / SummaryChunk / SummaryFinish 与对应 hook
- [x] 增加 stream -> accumulator -> hook 联调测试

## P1：把已有能力接入运行时入口

- [x] 让 EnhancedReActAgent 正式实现 IStateModule，并补 Save / Load / LoadIfExists 回归测试
- [x] 将 ToolGroupManager 接入 ReActAgent / EnhancedReActAgent 的实际工具选择流程
- [x] 扩展 ToolFactory，纳入已稳定的 read_file / write_file / shell_command 工具入口
- [x] 明确默认工具集与高级工具集的边界
- [x] 将集成测试拆分为快速验证与外部依赖验证，并为外部路径提供 smoke / Full 两级入口
- [x] 为 WebSocket transport 增加测试与最小可运行示例

## P2：补齐 Skill 与 MCP 的完整链路

- [x] 补 MarkdownSkillParser，并让 FileSystemSkillRepository 支持默认 MarkdownSkill 装载
- [x] 补 Skill 装载、绑定、启停链路
- [x] 设计并实现 SkillBox 或等价运行时装配能力
- [ ] 在 SkillBox 基础上补字段校验、richer metadata、绑定规则与更深运行时集成
- [x] 接入至少一个真实 MCP client（stdio）
- [x] 为 stdio MCP client 增加真实子进程回归测试
- [x] 补 MCP manager、content converter、错误映射
- [x] 为 MCP 增加端到端测试
- [ ] 补 MCP 多客户端命名隔离、更多 server 兼容性与更高层运行时入口

## P3：把骨架能力变成可运行能力

- [ ] 引入至少一个真实 TTS provider
- [ ] 明确 AudioPlayer 的跨平台策略
- [ ] 将 OpenAIMultiModalTool 从占位实现改为可配置真实调用
- [ ] 评估是否需要 TTSHook / Realtime TTS 路线
- [ ] 决定多模态能力是否进入统一工具入口

## P4：补生态与对外可用性

- [ ] 新增至少 3 个示例项目
- [ ] 建立独立文档站点
- [ ] 补中英文使用指南
- [ ] 将 capability scan 与 smoke tests 接入 CI gate
- [ ] 让状态文档、TODO、示例与实际发布版本同步更新

## 完成定义

以后新增或补完一项能力，默认同时满足以下条件才可标记为完成：

- [ ] 有稳定 API 与最小实现
- [ ] 有测试覆盖核心路径
- [ ] 已接入主链路或统一入口
- [ ] 依赖外部 provider 的能力具备真实实现、配置说明和错误处理
- [ ] 文档口径与实际行为一致