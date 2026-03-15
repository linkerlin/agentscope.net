// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Model;
using Xunit;

namespace AgentScope.Core.Tests.Model;

public class StructuredOutputReminderTests
{
    [Fact]
    public void ToolChoice_And_SystemPrompt_AreSingletons()
    {
        Assert.Same(StructuredOutputReminder.ToolChoice, StructuredOutputReminder.ToolChoice);
        Assert.Same(StructuredOutputReminder.SystemPrompt, StructuredOutputReminder.SystemPrompt);
        Assert.Equal("tool_choice", StructuredOutputReminder.ToolChoice.Kind);
        Assert.Equal("system_prompt", StructuredOutputReminder.SystemPrompt.Kind);
    }
}
