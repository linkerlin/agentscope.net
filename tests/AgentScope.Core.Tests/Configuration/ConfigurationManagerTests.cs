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

using Xunit;
using AgentScope.Core.Configuration;
using System;
using System.IO;

namespace AgentScope.Core.Tests.Configuration;

public class ConfigurationManagerTests
{
    [Fact]
    public void ConfigurationManager_Get_WithDefaultValue_ShouldReturnDefault()
    {
        // Arrange
        var key = "NON_EXISTENT_KEY_" + Guid.NewGuid();

        // Act
        var value = ConfigurationManager.Get(key, "default_value");

        // Assert
        Assert.Equal("default_value", value);
    }

    [Fact]
    public void ConfigurationManager_GetDatabasePath_ShouldReturnDefaultPath()
    {
        // Act
        var path = ConfigurationManager.GetDatabasePath();

        // Assert
        Assert.NotNull(path);
        Assert.Contains("agentscope", path);
    }

    [Fact]
    public void ConfigurationManager_GetMaxIterations_ShouldReturnDefaultValue()
    {
        // Act
        var maxIterations = ConfigurationManager.GetMaxIterations();

        // Assert
        Assert.True(maxIterations > 0);
    }

    [Fact]
    public void ConfigurationManager_GetDefaultModel_ShouldReturnModelName()
    {
        // Act
        var model = ConfigurationManager.GetDefaultModel();

        // Assert
        Assert.NotNull(model);
        Assert.NotEmpty(model);
    }

    [Fact]
    public void ConfigurationManager_GetLogLevel_ShouldReturnValidLevel()
    {
        // Act
        var logLevel = ConfigurationManager.GetLogLevel();

        // Assert
        Assert.NotNull(logLevel);
        Assert.NotEmpty(logLevel);
    }

    [Fact]
    public void ConfigurationManager_Load_WithNonExistentFile_ShouldNotThrow()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non_existent_{Guid.NewGuid()}.env");

        // Act & Assert - Should not throw
        ConfigurationManager.Load(nonExistentPath);
    }
}
