# AgentScope.NET TODO (校准版 2026-08-17)

## 当前基线

- 版本: v2.0.1 (develop/v2.0.1)
- 核心模块: 22/22 全部完成
- 代码: 959 .cs / ~65,941 行
- Core 构建: ✅ 0 错误 | 完整方案: 🔴 118 错误 / 230 警告
- 集成测试: ✅ 22 通过

## P0：修复构建阻塞

- [ ] **修复测试 Mock 类** (111 处 CS0535/CS0115)
  - AgentGroupTests.cs — TestAgent 实现新 IAgent 成员
  - AgentCoordinatorTests.cs — TestAgent 实现新接口成员
  - AgentRouterTests.cs — TestAgent 实现新接口成员
  - PipelineTests.cs — FakeAgent 实现新接口成员
  - SubAgentToolTests.cs — EchoAgent 修复 DoCallAsync/Call 签名
- [ ] **修复 Uno XAML 绑定** (7 处 CS0103)
  - MainWindow.xaml.cs: InputBox/SendButton/ChatListView x:Name

## P1：清理技术债务

- [ ] **升级 SQLitePCLRaw.lib.e_sqlite3** — 当前 2.1.11, 修复 NU1903
- [ ] **清理 CS0618 废弃 API** — GenerateOptions → 新类型, ReActAgent → EnhancedReActAgent
- [ ] **修复可空引用警告** — CS8601/CS8604 多处
- [ ] **修复 CA2024 reader.EndOfStream** — 4 处异步使用同步属性
- [ ] **修复 CS8425** — [EnumeratorCancellation] 缺失

## P2：补齐剩余链路

- [ ] MCP 多客户端命名隔离
- [ ] Skill 字段校验/richer metadata/绑定规则深化

## P3：骨架能力完善

- [ ] 真实 TTS provider
- [ ] OpenAIMultiModalTool 从占位改为真实调用

## P4：生态

- [ ] 新增至少 3 个示例项目
- [ ] 建立独立文档站点
- [ ] CI gate 集成

## 完成定义

标记完成需同时满足: 稳定 API + 测试覆盖 + 主链路接入 + 真实 provider(如适用) + 文档一致
