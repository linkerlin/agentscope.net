---
title: "Tool"
description: "Define, register, and manage the capabilities an agent can call"
---

## Overview

Tools are how an agent acts on the world — running business operations, calling APIs, reading and writing data. Each tool exposes itself to the LLM as a JSON Schema, and the agent invokes it through a unified interface.

AgentScope organizes tool-related building blocks under three concepts:

- **Tool** — any object implementing the `IAgentTool` contract (typically by extending `ToolBase`) or any plain class whose methods are annotated with `[Tool]`. .NET refers to the latter as *reflective function tools* — `Toolkit.RegisterTool(Object)` registers them by reflection automatically.
- **Toolkit** — the container that registers tools, MCP clients, and skills, exposes their JSON schemas to the model, and dispatches each tool call to the matching tool object.
- **Tool Group** — a named bundle of tools / MCP clients / skills that can be activated or deactivated as a unit. The agent uses a built-in meta tool to switch groups at runtime, keeping the context focused.

```csharp
using AgentScope.Core.Tool;
using AgentScope.Core.Tool.Builtin;

Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new TodoTools());
toolkit.RegisterTool(new MyCustomTools());
```

When you only call `RegisterTool(Object)`, every `[Tool]` method on the registered object joins the reserved `"basic"` group — always active. Add MCP clients, tool groups, or skills to extend the agent further — see the sections below.

## .NET tools

A .NET tool is any object satisfying the `IAgentTool` contract. AgentScope ships an abstract base class `ToolBase` for declaring tools with explicit parameter schemas, plus a reflective adapter that wraps plain methods into tools.

### IAgentTool / ToolBase contract

`ToolBase` is the abstract `IAgentTool` implementation. The table below lists its properties and methods.

Properties exposed to the agent and runtime:

| Method | Type | Description |
|--------|------|-------------|
| `GetName()` | `string` | The tool name shown to the agent |
| `GetDescription()` | `string` | Description shown to the agent |
| `GetParameters()` | `Dictionary<string, object>` | JSON Schema describing the parameters |
| `IsConcurrencySafe()` | `bool` | Can the tool be called concurrently? |
| `IsReadOnly()` | `bool` | Is the tool read-only / side-effect-free? |
| `IsExternalTool()` | `bool` | When `true`, execution is delegated externally (see [external execution](#external-execution-tools)) |
| `IsStateInjected()` | `bool` | When `true`, the framework injects `AgentState` as an extra parameter |
| `IsMcp()` | `bool` | Did the tool come from an MCP server? |
| `GetMcpName()` | `string` | The MCP server name when `IsMcp()` is `true` |

Methods that integrate with the execution flow and the permission system:

| Method | Required | Description |
|--------|----------|-------------|
| `CheckPermissions(toolInput, context)` | yes | Runtime permission check before execution; returns `Task<PermissionDecision>` |
| `MatchRule(ruleContent, toolInput)` | optional | Custom rule matcher for the permission system; returns `bool` |
| `GenerateSuggestions(toolInput)` | optional | Generate suggested rules from the current invocation; returns `List<PermissionRule>` |
| `CallAsync(param)` | optional | Tool execution; returns `Task<ToolResultBlock>`. External tools do not implement this. |

### Built-in tools

AgentScope currently ships these built-in tools:

| Tool | Description | Read-only |
|------|-------------|-----------|
| `TodoTools.TodoWrite` | Maintain a structured task list for the current session (full-list-replace semantics) | no |

Usage:

```csharp
Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new AgentScope.Core.Tool.Builtin.TodoTools());
```

:::{note}
The `Toolkit` automatically registers the `reset_tools` meta tool and the `load_skill_through_path` skill viewer tool when extra tool groups or skills are present — you don't need to instantiate them manually. See [self-managed tools](#self-managed-tools) and [Skill](#skill).
:::

### Custom tools (annotation-based)

The lightest-weight way: annotate plain methods with `[Tool]` and `[ToolParam]`, then call `Toolkit.RegisterTool(Object)`. The framework derives the JSON schema from C# types and the `Description` for the agent.

```csharp
using AgentScope.Core.Tool;
using System;

public class SimpleTools
{
    [Tool(
            Name = "get_current_time",
            Description = "Returns the current time in a given IANA timezone.",
            ReadOnly = true,
            ConcurrencySafe = true)]
    public string GetCurrentTime(
            [ToolParam(Name = "timezone", Description = "IANA timezone, e.g. Asia/Shanghai")]
                    string timezone)
    {
        return DateTime.UtcNow.ToString("o");
    }
}

Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new SimpleTools());
```

Common `[Tool]` attributes:

| Attribute | Type | Description |
|-----------|------|-------------|
| `Name` | `string` | Tool name (defaults to the method name) |
| `Description` | `string` | Description shown to the agent |
| `ReadOnly` | `bool` | Whether the tool is read-only (default `false`) |
| `ConcurrencySafe` | `bool` | Whether the tool is safe for concurrent calls (default `false`) |
| `StateInjected` | `bool` | Inject `AgentState` as an extra parameter (default `false`) |
| `DangerousFiles` / `DangerousDirectories` | `string[]` | Append custom dangerous paths |
| `Converter` | `Type` | Custom conversion of return values into `ToolResultBlock` |

### Custom tools (extending `ToolBase`)

When you need a custom permission policy, external execution, or a more complex schema, extend `ToolBase`:

```csharp
using AgentScope.Core.Message;
using AgentScope.Core.Permission;
using AgentScope.Core.Tool;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class WebSearchTool : ToolBase
{
    public WebSearchTool()
        : base(
                ToolBase.Builder()
                        .Name("WebSearch")
                        .Description("Search the web for information on a given query.")
                        .InputSchema(new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["query"] = new Dictionary<string, object>
                                {
                                    ["type"] = "string",
                                    ["description"] = "The search query."
                                }
                            },
                            ["required"] = new List<string> { "query" }
                        })
                        .ReadOnly(true)
                        .ConcurrencySafe(true))
    {
    }

    public override Task<PermissionDecision> CheckPermissions(
            Dictionary<string, object> toolInput, ToolExecutionContext context)
    {
        return Task.FromResult(PermissionDecision.Allow("Web search is read-only."));
    }

    public override async Task<ToolResultBlock> CallAsync(ToolCallParam param)
    {
        string query = (string)param.GetInput()["query"];
        string text = await DoSearchAsync(query);
        return ToolResultBlock.Builder()
                .Id(param.GetId())
                .Name(GetName())
                .Output(new List<ContentBlock> { TextBlock.Builder().Text(text).Build() })
                .Build();
    }

    private Task<string> DoSearchAsync(string query)
    {
        // ... actual search implementation
        return Task.FromResult("");
    }
}
```

### External execution tools

External-execution tools delegate the actual work outside the agent runtime — typically to a human operator or an external system. The agent emits `RequireExternalExecutionEvent` and pauses. When the next call feeds back matching `ToolResultBlock`s, the agent emits `ExternalExecutionResultEvent` with the same `ReplyId` before continuing.

This pattern is the foundation of [human-in-the-loop](./agent.md#human-in-the-loop) flows — some actions need human approval or human execution.

To create an external tool, set `ExternalTool` to `true` and skip implementing `CallAsync`:

```csharp
using AgentScope.Core.Permission;
using AgentScope.Core.Tool;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class HumanApprovalTool : ToolBase
{
    public HumanApprovalTool()
        : base(
                ToolBase.Builder()
                        .Name("HumanApproval")
                        .Description("Request human approval for a sensitive operation.")
                        .InputSchema(new Dictionary<string, object>
                        {
                            ["type"] = "object",
                            ["properties"] = new Dictionary<string, object>
                            {
                                ["action"] = new Dictionary<string, object> { ["type"] = "string" },
                                ["reason"] = new Dictionary<string, object> { ["type"] = "string" }
                            },
                            ["required"] = new List<string> { "action", "reason" }
                        })
                        .ReadOnly(false)
                        .ConcurrencySafe(true)
                        .ExternalTool(true))
    {
    }

    public override Task<PermissionDecision> CheckPermissions(
            Dictionary<string, object> toolInput, ToolExecutionContext context)
    {
        return Task.FromResult(PermissionDecision.Allow("External tool dispatch is always allowed."));
    }
}
```

Runnable examples: `agentscope-examples/documentation/.../tool/ToolBaseExample.cs`, `tool/ToolExecutionContextExample.cs`.

## Receiving context

The [`RuntimeContext`](./agent.md#runtimecontext-per-call-context) passed to `agent.CallAsync(msgs, runtimeContext)` is forwarded to every tool invocation in that reply. Tools can read it in two ways: annotation-based tools through automatic injection, and `ToolBase.CallAsync` through `ToolCallParam`.

### Automatic injection (`[Tool]` methods)

Inside a `[Tool]` method, any parameter **without `[ToolParam]`** is treated as framework-injected. The resolution order:

| Parameter type | Source |
|----------------|--------|
| `ToolEmitter` | Streaming emitter (no-op when none configured) |
| `IAgent` | The current agent instance |
| `AgentState` | The per-session state for the current call (via `RuntimeContext.GetAgentState()`) |
| `RuntimeContext` | The current per-call context |
| `ToolExecutionContext` | `runtimeContext.AsToolExecutionContext()` (compatibility shim, deprecated) |
| Any other user POCO type | `runtimeContext.Get<T>()` — i.e. an object the caller registered via `RuntimeContext.Builder().Put<T>(value)` |

"User POCO" means: no `[ToolParam]`, not primitive, not `ContentBlock` / `Msg`, not under `System.*`. Every other parameter (those with `[ToolParam]`, or that fall outside the above types) is read from the LLM-supplied JSON by name.

```csharp
using AgentScope.Core.Tool;

public record UserContext(string Username, string Locale);

public class PersonalizedTools
{
    [Tool(Name = "greet", Description = "Greet the user with a custom greeting")]
    public string Greet(
            [ToolParam(Name = "greeting", Description = "Greeting word, e.g. 'Hello'")]
                    string greeting,                  // ← supplied by the model
            UserContext userCtx)                      // ← injected by the framework
    {
        return greeting + ", " + (userCtx?.Username ?? "unknown") + "!";
    }
}
```

The caller registers the POCO by type once; every `CallAsync` then routes the matching instance to any tool that asks for it:

```csharp
RuntimeContext ctx =
        RuntimeContext.Builder()
                .Put<UserContext>(new UserContext("alice", "en"))
                .UserId("alice")
                .Build();

await agent.CallAsync(new List<Msg> { new UserMessage("Greet me.") }, ctx);
```

The model never sees `userCtx` — it is not part of the tool's JSON schema. Full example: `agentscope-examples/documentation/.../tool/ToolExecutionContextExample.cs`.

### Accessing context in `ToolBase.CallAsync`

Tools that extend `ToolBase` read context through `ToolCallParam`:

```csharp
using AgentScope.Core.Agent;
using AgentScope.Core.Tool;
using System.Threading.Tasks;

public class TenantAwareTool : ToolBase
{
    public TenantAwareTool() : base(/* builder ... */) { }

    public override Task<ToolResultBlock> CallAsync(ToolCallParam param)
    {
        RuntimeContext rc = param.GetRuntimeContext();
        string tenantId = rc?.GetUserId();
        TenantConfig cfg = rc?.Get<TenantConfig>();
        // ... apply tenantId / cfg ...
        return Task.FromResult(/* ... */);
    }
}
```

`ToolCallParam` also exposes `GetAgent()`, `GetInput()`, `GetEmitter()`, `GetToolUseBlock()`, and the deprecated `GetContext()`. Prefer `GetRuntimeContext()` in new code.

### Coordinating between hooks and tools

The `RuntimeContext` string layer (`Put(string, object)` / `Get(string)`) is a short-lived channel between middleware and tools during a single `CallAsync` — a middleware can write at `OnActing`/`OnReasoning` and a tool that injects a `RuntimeContext` parameter reads it. The instance is unbound from the agent (along with all hooks) when the call finishes.

## MCP

AgentScope integrates with the [Model Context Protocol (MCP)](https://modelcontextprotocol.io/), letting the agent talk to any MCP-compatible tool provider. The framework handles protocol negotiation, tool discovery, and result conversion.

Three transports are supported:

- **STDIO** — local process via stdin/stdout
- **SSE / Streamable HTTP** — remote HTTP long-connection

MCP tools are exposed in the toolkit under the namespace `mcp__{server_name}__{tool_name}` to avoid collisions; tools marked `ReadOnlyHint` are auto-allowed by the permission system.

### Registering MCP tools

Use `McpClientBuilder` to build an `McpClientWrapper`, then register it on the `Toolkit`:

::::{tab-set}
:::{tab-item} STDIO
```csharp
using AgentScope.Core.Tool;
using AgentScope.Core.Tool.Mcp;

McpClientWrapper filesystem =
        McpClientBuilder.Stdio()
                .Name("filesystem")
                .Command("mcp-server-filesystem")
                .Args("--root", "/my/project")
                .Build();

Toolkit toolkit = new Toolkit();
await toolkit.RegisterMcpClientAsync(filesystem);
```
:::
:::{tab-item} Streamable HTTP
```csharp
using AgentScope.Core.Tool;
using AgentScope.Core.Tool.Mcp;

McpClientWrapper weather =
        McpClientBuilder.StreamableHttp()
                .Name("weather")
                .Url("https://api.weather.com/mcp")
                .Header("Authorization", "Bearer xxx")
                .Build();

Toolkit toolkit = new Toolkit();
await toolkit.RegisterMcpClientAsync(weather);
```
:::
:::{tab-item} SSE
```csharp
using AgentScope.Core.Tool.Mcp;

McpClientWrapper search =
        McpClientBuilder.Sse()
                .Name("search")
                .Url("https://api.search.com/mcp/sse")
                .Build();

Toolkit toolkit = new Toolkit();
await toolkit.RegisterMcpClientAsync(search);
```
:::
::::

Runnable examples: `agentscope-examples/documentation/.../mcp/McpStdioExample.cs`, `mcp/McpSseExample.cs`, `mcp/McpStreamableHttpExample.cs`.

## Skill

Skills are markdown-based instruction sets that extend an agent's capabilities without writing new tool code. Each skill is a directory containing a `SKILL.md` file with frontmatter metadata and detailed instructions.

Unlike tools, skills are not directly callable. The agent reads skill instructions through an auto-registered viewer tool named `load_skill_through_path`, then carries them out using whatever tools it already has.

### Registering skills

Attach one or more `IAgentSkillRepository` directly via `ReActAgent.Builder().SkillRepository(...)`. At `Build()` time the builder auto-installs `DynamicSkillMiddleware`, which rebuilds the skill prompt and tool groups from the configured sources on every `CallAsync()`:

```csharp
using AgentScope.Core;
using AgentScope.Core.Skill.Repository;
using System.IO;

ReActAgent agent =
        ReActAgent.Builder()
                .Name("SkillCreator")
                .SysPrompt("...")
                .Model(model)
                .SkillRepository(new FileSystemSkillRepository(Path.GetFullPath("/path/to/skills"), false))
                .Build();
```

Multiple `SkillRepository(...)` calls append in order (low → high priority); when two repositories expose a skill with the same name, the later entry wins. Use `SkillRepositories(List<IAgentSkillRepository>)` to replace the list.

Reference implementations: `agentscope-examples/documentation/.../skill/AgentSkillExample.cs`, `skill/SkillWithToolGroupExample.cs`.

### How skills work

When skills are present, the `Toolkit` performs a two-phase setup.

Initialisation:

- The toolkit scans every registered skill source and collects each skill's name, description, and directory.
- It auto-registers the built-in viewer tool `load_skill_through_path` (implemented in `AgentScope.Core.Skill.SkillToolFactory`) into the `skill-build-in-tools` group.
- It assembles a system-prompt fragment listing the available skills (names + descriptions) and instructing the agent to read full content via `load_skill_through_path`.

At runtime, the agent invokes the viewer with two required arguments:

| Parameter | Type | Description |
| --- | --- | --- |
| `SkillId` | `string` (enum of registered skill IDs) | The skill to load. |
| `Path` | `string` | Use `"SKILL.md"` to fetch the skill's markdown instructions, or an exact resource path declared by the skill such as `"references/guide.md"` or `"scripts/run.py"`. Do not pass `"."`, `"./"`, a directory, or an absolute path. |

Example tool call payload:

```json
{
  "name": "load_skill_through_path",
  "input": { "skillId": "pdf-extractor", "path": "SKILL.md" }
}
```

Each successful call has two effects:

1. Returns the requested content (the `SKILL.md` markdown, or the named resource file).
2. **Activates the skill** — its associated tool group is enabled in the `Toolkit`, so any tools bundled with the skill become callable for the rest of the turn. If the requested `path` does not exist, the viewer returns an error that lists the available resource paths (with `SKILL.md` first) so the agent can retry.

:::{note}
A skill is not a tool — the agent cannot call it directly. The agent must read the instructions via `load_skill_through_path` first, then act on them with other tools.
:::

### Skill script execution: configuring shell tools

Skills only provide instructions — actual execution relies on the tools the agent already has. If a skill's instructions involve running scripts (e.g. `scripts/run.py`), the agent needs shell access:

- **`ReActAgent`** — register `ShellCommandTool` in the toolkit:

```csharp
using AgentScope.Core.Tool;
using AgentScope.Core.Tool.Coding;
using AgentScope.Core.Tool.File;

Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new ShellCommandTool());
toolkit.RegisterTool(new ReadFileTool("/path/to/base/dir"));
toolkit.RegisterTool(new WriteFileTool("/path/to/base/dir"));

ReActAgent agent =
        ReActAgent.Builder()
                .Name("SkillAgent")
                .SysPrompt("...")
                .Model(model)
                .Toolkit(toolkit)
                .SkillRepository(skillRepo)
                .Build();
```

- **`HarnessAgent`** — the harness module ships workspace-aware shell and file tools (`Execute`, `ReadFile`, `WriteFile`, etc.) out of the box; no extra registration needed.

### Skill + ToolGroup: on-demand tool disclosure

`SkillToolGroup` binds a group of tools to a skill name — the group activates automatically when the agent loads that skill, and stays hidden from the model's schema otherwise, reducing context noise.

```csharp
using AgentScope.Core;
using AgentScope.Core.Tool;

Toolkit toolkit = new Toolkit();

// 1. Create a tool group bound to a skill (initially inactive)
toolkit.CreateSkillToolGroup(
        "analysis-tools",                // group name
        "Data analysis tools",           // description
        false,                           // initially inactive
        "data-analysis");                // bound skill name

// 2. Register tools into that group
toolkit.Registration()
        .Tool(new AnalysisTools())
        .Group("analysis-tools")
        .Apply();

// 3. Build the agent with meta tool for model-driven group switching
ReActAgent agent =
        ReActAgent.Builder()
                .Name("AnalysisAgent")
                .SysPrompt("...")
                .Model(model)
                .Toolkit(toolkit)
                .SkillRepository(skillRepo)
                .EnableMetaTool(true)
                .Build();
```

When the agent loads the `data-analysis` skill via `load_skill_through_path`, the `analysis-tools` group activates and its tools become immediately available. With `EnableMetaTool(true)`, the model can also manage group activation via `reset_tools`.

Reference implementation: `agentscope-examples/documentation/.../skill/SkillWithToolGroupExample.cs`.

## Self-managed tools

The built-in **meta tool** (`reset_tools`) lets the agent self-manage which tool groups are active at runtime, keeping the context focused — only the tools relevant to the current task are exposed to the model.

### Defining tool groups

`ToolGroup` is a named bundle of tools / MCP clients / skills. Register the group on the `Toolkit` and turn on the meta tool through the builder:

```csharp
using AgentScope.Core;
using AgentScope.Core.Tool;

Toolkit toolkit = new Toolkit();
toolkit.RegisterTool(new BasicTools());

ToolGroup database =
        new ToolGroup(
                "database",
                "Tools for database operations.",
                ToolGroupScope.SESSION,
                /* active = */ false);
database.AddTool("db_query");
database.AddTool("db_migrate");
toolkit.RegisterTool(new DatabaseTools());
toolkit.RegisterToolGroup(database);

ToolGroup deployment =
        new ToolGroup(
                "deployment",
                "Tools for deploying services.",
                ToolGroupScope.SESSION,
                /* active = */ false);
deployment.AddTool("deploy");
deployment.AddTool("rollback");
toolkit.RegisterTool(new DeploymentTools());
toolkit.RegisterToolGroup(deployment);

ReActAgent agent =
        ReActAgent.Builder()
                .Name("router")
                .Toolkit(toolkit)
                .EnableMetaTool(true)
                .Build();
```

`ToolGroup` takes a name, a description, a scope (`ToolGroupScope`), and an initial active flag. The reserved name `"basic"` is auto-populated by `Toolkit.RegisterTool(Object)` and is always active.

### Using the meta tool

Whenever there's at least one non-basic tool group and `EnableMetaTool(true)` is on, the `Toolkit` auto-registers `reset_tools` and exposes its schema to the agent. Each non-basic group becomes a boolean field; calling the meta tool declares the desired final state.

Runtime behavior:

- Tools in the `"basic"` group are always exposed; the meta tool does not touch them.
- Each `reset_tools` call **wholly overwrites** the active set — any non-basic group not explicitly set to `true` is deactivated, regardless of its previous state.
- For each group that just became active, its description and (if provided) instructions are spliced into the meta tool's return value, telling the agent how to use it correctly.
- Tools in inactive groups do not appear in the agent's tool schema, leaving more context for the active toolset.

:::{warning}
The meta tool's input represents the **final state** of all groups, not a delta. Any group not explicitly set to `true` is deactivated regardless of previous state.
:::

## Further reading

::::{grid} 2

:::{grid-item-card} Agent
:link: ./agent.html

How agents orchestrate tool calls in the ReAct loop
:::
  :::{grid-item-card} Permission System
:link: ./permission-system.html

Fine-grained control over which tools execute and when
:::
  :::{grid-item-card} Middleware
:link: ./middleware.html

Use onion middlewares to intercept and rewrite tool calls
:::
  :::{grid-item-card} Human-in-the-Loop
:link: ./agent.html#human-in-the-loop

External execution tools and approval workflows
:::

::::
