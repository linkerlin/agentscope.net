# Contributing to AgentScope.NET

欢迎为 AgentScope.NET 做出贡献！/ Welcome to contribute to AgentScope.NET!

## 开发环境 Development Environment

### 要求 Requirements

- .NET 9.0 SDK or higher
- Visual Studio 2022, Visual Studio Code, or JetBrains Rider
- Git

### 克隆仓库 Clone Repository

```bash
git clone https://github.com/linkerlin/agentscope.net.git
cd agentscope.net
```

### 构建项目 Build Project

```bash
dotnet restore
dotnet build
```

### 运行测试 Run Tests

```bash
dotnet test
```

## 代码规范 Code Standards

### C# 编码风格 C# Coding Style

- 遵循 [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用 4 个空格缩进 / Use 4 spaces for indentation
- 使用 PascalCase 命名类和方法 / Use PascalCase for classes and methods
- 使用 camelCase 命名局部变量和参数 / Use camelCase for local variables and parameters
- 使用 \_camelCase 命名私有字段 / Use \_camelCase for private fields

### 注释 Comments

- 为公共 API 添加 XML 文档注释 / Add XML doc comments for public APIs
- 使用清晰的注释解释复杂的逻辑 / Use clear comments to explain complex logic
- 包含许可证头 / Include license headers

### 许可证头 License Header

每个文件开头应包含以下许可证头 / Each file should include the following license header:

```csharp
// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
```

## 提交流程 Contribution Process

### 1. Fork 仓库 Fork Repository

在 GitHub 上 fork 本仓库。/ Fork this repository on GitHub.

### 2. 创建分支 Create Branch

```bash
git checkout -b feature/your-feature-name
```

### 3. 进行更改 Make Changes

- 编写代码 / Write code
- 添加测试 / Add tests
- 更新文档 / Update documentation

### 4. 提交更改 Commit Changes

```bash
git add .
git commit -m "Add: your feature description"
```

提交消息格式 / Commit message format:
- `Add: 添加新功能 / Add new feature`
- `Fix: 修复问题 / Fix bug`
- `Update: 更新功能 / Update feature`
- `Refactor: 重构代码 / Refactor code`
- `Docs: 更新文档 / Update documentation`
- `Test: 添加测试 / Add test`

### 5. 推送到 Fork Push to Fork

```bash
git push origin feature/your-feature-name
```

### 6. 创建 Pull Request Create Pull Request

在 GitHub 上创建 Pull Request。/ Create a Pull Request on GitHub.

## 测试指南 Testing Guidelines

### 单元测试 Unit Tests

- 为新功能添加单元测试 / Add unit tests for new features
- 确保测试覆盖率 / Ensure test coverage
- 使用 xUnit 作为测试框架 / Use xUnit as test framework

### 集成测试 Integration Tests

- 为关键流程添加集成测试 / Add integration tests for critical flows
- 测试与外部服务的集成 / Test integration with external services

## 文档 Documentation

### API 文档 API Documentation

- 为公共 API 添加 XML 文档注释 / Add XML doc comments for public APIs
- 包含参数说明和返回值说明 / Include parameter and return value descriptions
- 提供使用示例 / Provide usage examples

### README 更新 README Updates

- 更新 README.md 以反映新功能 / Update README.md to reflect new features
- 添加使用示例 / Add usage examples
- 更新路线图 / Update roadmap

## 问题报告 Issue Reporting

### Bug 报告 Bug Report

提交 Bug 时请包含：/ When reporting bugs, please include:

1. 问题描述 / Problem description
2. 复现步骤 / Steps to reproduce
3. 预期行为 / Expected behavior
4. 实际行为 / Actual behavior
5. 环境信息 / Environment information
6. 错误日志 / Error logs

### 功能请求 Feature Request

提交功能请求时请包含：/ When requesting features, please include:

1. 功能描述 / Feature description
2. 使用场景 / Use cases
3. 预期效果 / Expected outcome
4. 可能的实现方案 / Possible implementation

## 代码审查 Code Review

所有 Pull Request 都需要经过代码审查。/ All Pull Requests require code review.

审查重点：/ Review focus:

- 代码质量 / Code quality
- 测试覆盖率 / Test coverage
- 文档完整性 / Documentation completeness
- 性能影响 / Performance impact
- 安全性 / Security

## 社区行为准则 Code of Conduct

- 尊重他人 / Respect others
- 保持专业 / Be professional
- 建设性沟通 / Communicate constructively
- 接受反馈 / Accept feedback

## 联系方式 Contact

- GitHub Issues: https://github.com/linkerlin/agentscope.net/issues
- Email: (待添加 / To be added)

感谢您的贡献！/ Thank you for your contribution!