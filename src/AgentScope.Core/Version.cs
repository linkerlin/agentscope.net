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

namespace AgentScope.Core;

/// <summary>
/// Version information for AgentScope.NET
/// AgentScope.NET 版本信息
/// </summary>
public static class Version
{
    /// <summary>
    /// Current version string (SemVer).
    /// 当前版本号（语义化版本）
    /// </summary>
    public const string VERSION = "2.0.1";

    /// <summary>
    /// Build date in ISO 8601 format (yyyy-MM-dd).
    /// 构建日期（ISO 8601 格式 yyyy-MM-dd）
    /// </summary>
    public const string BUILD_DATE = "2026-08-17";
    
    /// <summary>
    /// Gets the version string only.
    /// 仅获取版本号
    /// </summary>
    public static string GetVersion()
    {
        return VERSION;
    }
    
    /// <summary>
    /// Gets the full version string including product name and build date.
    /// 获取包含产品名和构建日期的完整版本字符串
    /// </summary>
    public static string GetFullVersion()
    {
        return $"AgentScope.NET {VERSION} (Built on {BUILD_DATE})";
    }
}
