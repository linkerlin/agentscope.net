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

namespace AgentScope.Core.Tool.Coding;

/// <summary>
/// 命令校验器：白名单/黑名单等，用于 ShellCommandTool 安全执行。
/// </summary>
public interface ICommandValidator
{
    /// <summary>
    /// 校验命令是否允许执行。
    /// </summary>
    bool Validate(string command);
}
