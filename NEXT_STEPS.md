# 当前工作重点 (2026-08-17)

**版本**: v2.0.1 | **分支**: develop/v2.0.1
**Core 构建**: ✅ 0 错 | **完整方案**: 🔴 118 错 / 230 警告

## P0 — 必须立即修复

### 1. 测试 Mock 类 (111 处 CS0535/CS0115)
测试内部类未实现 IAgent/ICallableAgent/IStreamableAgent/IObservableAgent 新增成员:

| 文件 | 需要实现 |
|------|---------|
| `AgentGroupTests.cs:225` | CallAsync(IReadOnlyList<Msg>,RuntimeContext?), CallAsync(string,RuntimeContext?), StreamEventsAsync, ObserveAsync, Interrupt(), AgentId, Description |
| `AgentCoordinatorTests.cs:219` | 同上 |
| `AgentRouterTests.cs:290` | 同上 |
| `PipelineTests.cs:515` | 同上 |
| `SubAgentToolTests.cs:64-67` | DoCallAsync(IReadOnlyList<Msg>), Call(Msg) → 方法签名变更 |

### 2. Uno XAML (7 处 CS0103)
`MainWindow.xaml.cs`: InputBox/SendButton/ChatListView 的 x:Name 引用未绑定到 XAML 元素

## P1 — 重要清理

3. **NU1903 SQLitePCLRaw 漏洞** - 升级 2.1.11
4. **CS0618 废弃 API** - GenerateOptions → 新类型, ReActAgent → EnhancedReActAgent
5. **CS8601 可空引用** - 多处警告

## P2 — 增强

6. 更多示例项目 (目前 2 个)
7. 独立文档站点
8. CI gate 集成

## 关键文档
- [TODO.md](./TODO.md) — 校准版待办
- [CURRENT_STATUS.md](./CURRENT_STATUS.md) — 详细状态
- `docs/Java对齐差距分析及改造建议.md`
