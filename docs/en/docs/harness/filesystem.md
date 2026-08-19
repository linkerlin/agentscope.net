---
title: "Filesystem"
description: "Three deployment modes: local + shell / shared store / sandbox; IsolationScope dimensions; multi-user isolation; how skills and tools behave in each mode"
---

## Role

`HarnessAgent` abstracts the agent's view of the **workspace** away from "must be local disk" into a uniform interface. All file tools (`read_file` / `write_file` / `edit_file` / `grep_files` / `glob_files` / `list_files`) and the optional `execute` (shell) go through this abstraction.

The payoff: you can switch between three deployment modes **without changing agent code**:

- Local + shell — single process, local, trusted env;
- Shared store — multiple replicas / pods share the same long-term memory;
- Sandbox — files and commands run in an isolated container; the same workspace state is restored across calls.

## Three declarative modes

Pick one with `filesystem(...)` on `HarnessAgent.Builder` (no call = mode 3 by default):

| Mode | Config | Shell? | When to use |
|------|--------|--------|-------------|
| **1 · Shared store** | `filesystem(new RemoteFilesystemSpec(store))` | No | Multiple replicas share `MEMORY.md` / conversation logs / subtask records via KV; **no shell on the host** |
| **2 · Sandbox** | `filesystem(new DockerFilesystemSpec()...)`, or K8s / Daytona / E2B / AgentRun | Yes (inside sandbox) | Isolated execution, cross-call workspace recovery, optional snapshots + distributed |
| **3 · Local + shell** (default) | `filesystem(new LocalFilesystemSpec()...)` or **omit it** | Yes (host `sh -c`) | Single process / local / trusted env / scripts and tests |

> `filesystem(...)` is mutually exclusive with `abstractFilesystem(...)`; the latter is an escape hatch for fully self-managed filesystems and rarely needed.

---

### Mode 1: shared store (`RemoteFilesystemSpec`)

For "multi-replica, but the user's long-term memory must stay in sync". Pass a `BaseStore` implementation (Redis / JDBC / in-memory) and the framework automatically routes workspace files into the KV store by path prefix:

```csharp
// minimal config (recommended: use DistributedStore for one-line setup)
DistributedStore store = RedisDistributedStore.FromJedis(jedis);

HarnessAgent agent = HarnessAgent.Builder()
    .Name("store-agent")
    .Model(model)
    .Workspace(workspace)
    .DistributedStore(store)
    .Filesystem(new RemoteFilesystemSpec()   // baseStore auto-injected from store
        .IsolationScope(IsolationScope.USER))
    .Build();
```

#### All configuration options

| Method | Description | Default |
|--------|-------------|---------|
| `IsolationScope(IsolationScope)` | Namespace isolation dimension (see [IsolationScope](#isolationscope--bucketing-across-users-and-replicas) below) | `USER` |
| `AnonymousUserId(string)` | Fallback identifier when `userId` is absent | `"_default"` |
| `AddSharedPrefix(string)` | Route additional workspace-relative prefixes to the KV (e.g. `"prompts/"`, `"configs/"`) | none |
| `WorkspaceIndex(WorkspaceIndex)` | SQLite index to accelerate remote ls/glob/grep | none (falls back to full store scan) |

#### Built-in routing rules

The framework automatically routes the following paths to the shared KV, each in its own namespace segment to prevent key collisions:

| Path | KV namespace segment |
|------|---------------------|
| `AGENTS.md`, `MEMORY.md`, `tools.json` | `root` |
| `memory/` | `memory` |
| `skills/` | `skills` |
| `subagents/` | `subagents` |
| `knowledge/` | `knowledge` |
| `agents/<agentId>/sessions/` | `sessions` |
| `agents/<agentId>/tasks/` | `tasks` |

Paths not in the table above fall through to a local `LocalFilesystem` (no shell).

#### Example: multi-replica customer-service agent

Three pods each running a `HarnessAgent`, sharing one Redis as the `BaseStore`:

```csharp
DistributedStore store = RedisDistributedStore.FromJedis(
        new JedisPooled("redis://shared-redis:6379"));

HarnessAgent agent = HarnessAgent.Builder()
    .Name("customer-service")
    .Model(model)
    .Workspace(Path.GetFullPath("/opt/agent/workspace"))
    .DistributedStore(store)                  // stateStore + baseStore in one call
    .Filesystem(new RemoteFilesystemSpec()
        .IsolationScope(IsolationScope.USER)      // one namespace per user
        .AnonymousUserId("anonymous"))            // fallback for unauthenticated callers
    .Build();
```

- Each pod's local `AGENTS.md` / `knowledge/` / `skills/` serve as read-only templates (git-synced);
- Runtime outputs (`MEMORY.md`, `memory/`, conversation logs) are stored in Redis automatically — any pod reads the latest state;
- Alice's memory lives under KV key `agents/customer-service/users/alice/memory/...`.

This mode **does not provide shell** — on purpose: for shell, use mode 2 (sandbox) or 3 (local).

#### Available `BaseStore` implementations

| Implementation | Description |
|---------------|-------------|
| `RedisStore` | StackExchange.Redis-based, for low-latency high-concurrency |
| `JdbcStore` | ADO.NET-based, for MySQL / PostgreSQL / SQLite |
| `InMemoryStore` | In-memory, for testing |

---

### Mode 2: sandbox (`SandboxFilesystemSpec` family)

For "code may run untrusted operations" or "isolate from the production host". Every file op and shell command goes to the sandbox; the host is untouched.

#### Docker sandbox

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("sandbox-agent")
    .Model(model)
    .Workspace(workspace)
    .Filesystem(new DockerFilesystemSpec()
        .Image("ubuntu:24.04")
        .IsolationScope(IsolationScope.SESSION)
        .MemorySizeBytes(512 * 1024 * 1024L)   // 512 MB memory limit
        .CpuCount(2L)
        .Network("host")
        .ExposedPorts(8080, 3000)
        .Environment(new Dictionary<string, string> { ["NODE_ENV"] = "development" })
        .SnapshotSpec(new LocalSnapshotSpec("/data/snapshots")))
    .Build();
```

`DockerFilesystemSpec` — all options:

| Method | Description | Default |
|--------|-------------|---------|
| `Image(string)` | Docker image | required |
| `IsolationScope(IsolationScope)` | Isolation dimension | `SESSION` |
| `MemorySizeBytes(long)` | Container memory limit | Docker default |
| `CpuCount(long)` | CPU limit | Docker default |
| `Network(string)` | Docker network | Docker default |
| `ExposedPorts(params int[])` | Exposed ports | none |
| `Environment(Dictionary<string,string>)` | Container environment variables | none |
| `WorkspaceRoot(string)` | Workspace mount point inside the container | `/workspace` |
| `AdditionalRunArgs(params string[])` | Extra `docker run` arguments | none |
| `SnapshotSpec(SandboxSnapshotSpec)` | Snapshot strategy | `NoopSnapshotSpec` (no snapshots) |
| `WorkspaceSpec(WorkspaceSpec)` | Workspace mount rules | default |
| `ExecutionGuard(SandboxExecutionGuard)` | Concurrency guard for AGENT / GLOBAL scope | none |
| `WorkspaceProjectionEnabled(bool)` | Enable host → sandbox static asset projection | `true` |
| `WorkspaceProjectionRoots(List<string>)` | Root paths included in projection | `AGENTS.md`, `skills`, `subagents`, `knowledge`, `.skills-cache` |

#### Kubernetes sandbox (agent-sandbox)

The Kubernetes store is fully based on [agent-sandbox](https://github.com/kubernetes-sigs/agent-sandbox): sandbox pods are managed by the agent-sandbox controller in your cluster, and image, resources, and PVCs are all declared cluster-side in a `SandboxTemplate` / `SandboxWarmPool` (not configured from C#). The C# side claims instances from the warm pool via `SandboxClaim`. Install the agent-sandbox controller and create the template and warm pool before use.

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("k8s-agent")
    .Model(model)
    .Workspace(workspace)
    .Filesystem(new KubernetesFilesystemSpec()
        .Namespace("agents")
        .WarmPoolName("agent-pool")        // SandboxWarmPool name
        .IsolationScope(IsolationScope.USER))
    .Build();
```

Main `KubernetesFilesystemSpec` options:

| Method | Description | Default |
|--------|-------------|---------|
| `Namespace(string)` | namespace of the SandboxClaim | `default` |
| `WarmPoolName(string)` | `SandboxWarmPool` name | required |
| `WorkspaceRoot(string)` | workspace root inside the sandbox; **must be on the PVC mount declared in the template** | `/workspace` |
| `FileApiBaseDir(string)` | runtime file API base directory; must match `workspaceRoot`; blank falls back to base64-over-exec transfer | `/workspace` |
| `ApiUrl(string)` | direct runtime API URL (takes precedence when set) | none |
| `GatewayName(string)` / `GatewayNamespace(string)` / `GatewayScheme(string)` | reach the sandbox through the Gateway API | none |
| `ServerPort(int)` | runtime HTTP API port | `8888` |
| `KubernetesClient(KubernetesClient)` | custom fabric8 client | kubeconfig auto-loaded |
| `SnapshotSpec(SandboxSnapshotSpec)` | snapshot strategy (see the sandbox page for the PVC trade-off) | `NoopSnapshotSpec` |

When neither `apiUrl` nor `gateway*` is set, a local tunnel via `kubectl port-forward` is used (good for development). The runtime image must satisfy the [runtime image contract](./sandbox.md#runtime-image-contract); **workspace persistence depends on the PVC configured in the template** — see [Sandbox - Kubernetes state persistence](./sandbox.md#kubernetes-state-persistence-pvc-is-the-first-layer).

#### E2B sandbox

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("e2b-agent")
    .Model(model)
    .Workspace(workspace)
    .Filesystem(new E2bFilesystemSpec()
        .ApiKey("${E2B_API_KEY}")
        .TemplateId("my-template")
        .SandboxTimeoutSeconds(300)
        .IsolationScope(IsolationScope.SESSION))
    .Build();
```

#### Daytona sandbox

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("daytona-agent")
    .Model(model)
    .Workspace(workspace)
    .Filesystem(new DaytonaFilesystemSpec()
        .ApiKey("${DAYTONA_API_KEY}")
        .ControlPlaneBaseUrl("https://api.daytona.io")
        .Image("python:3.12-slim")
        .Cpu(2)
        .Memory(4)        // GiB
        .Disk(10)         // GiB
        .IsolationScope(IsolationScope.USER))
    .Build();
```

#### AgentRun sandbox (Alibaba Cloud)

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("agentrun-agent")
    .Model(model)
    .Workspace(workspace)
    .Filesystem(new AgentRunFilesystemSpec()
        .ApiKey("${AGENTRUN_API_KEY}")
        .AccountId("your-account-id")
        .Region("cn-hangzhou")
        .TemplateName("python3.12")
        .SandboxIdleTimeoutSeconds(600)
        .IsolationScope(IsolationScope.USER))
    .Build();
```

#### Common options inherited from `SandboxFilesystemSpec`

| Method | Description | Default |
|--------|-------------|---------|
| `IsolationScope(IsolationScope)` | Isolation dimension | store-specific (usually `SESSION`) |
| `SnapshotSpec(SandboxSnapshotSpec)` | Snapshot strategy | `NoopSnapshotSpec` |
| `ExecutionGuard(SandboxExecutionGuard)` | Concurrency serialization guard for AGENT/GLOBAL scopes | none |
| `WorkspaceProjectionEnabled(bool)` | Project static assets from host to sandbox | `true` |
| `WorkspaceProjectionRoots(List<string>)` | Root paths to include in projection | `AGENTS.md`, `skills`, `subagents`, `knowledge`, `.skills-cache` |

#### Snapshot strategies

Snapshots let the next `call()` restore the previous sandbox state (installed deps, generated files, etc.):

| Implementation | Description |
|---------------|-------------|
| `NoopSnapshotSpec` | No snapshots (default) |
| `LocalSnapshotSpec(Path)` | Snapshots stored on host local disk |
| `RedisSnapshotSpec` | Snapshots stored in Redis |
| `OssSnapshotSpec` | Snapshots stored in object storage (Alibaba Cloud OSS) |
| `RemoteSnapshotSpec` | Snapshots stored in a `BaseStore` |

#### Example: coding assistant (Docker + local snapshots)

```csharp
HarnessAgent codingAgent = HarnessAgent.Builder()
    .Name("coder")
    .Model(model)
    .Workspace(Path.GetFullPath(".agentscope/workspace"))
    .Filesystem(new DockerFilesystemSpec()
        .Image("node:20-slim")
        .IsolationScope(IsolationScope.USER)
        .MemorySizeBytes(1024 * 1024 * 1024L)
        .SnapshotSpec(new LocalSnapshotSpec("/data/sandbox-snapshots")))
    .DistributedStore(store)
    .Build();

// Alice's first call: npm install inside sandbox, snapshot saved afterward
RuntimeContext rc = RuntimeContext.Builder()
    .UserId("alice")
    .SessionId("dev-session-1")
    .Build();
agent.Call(Msg.User("npm install && npm test"), rc).GetAwaiter().GetResult();

// Alice's second call: snapshot restored, node_modules still present
agent.Call(Msg.User("npm run build"), rc).GetAwaiter().GetResult();
```

#### Workspace projection

When a sandbox starts, the framework tars the workspace's "static assets" and hydrates them into `/workspace` inside the container. These include:

- `AGENTS.md` (persona file)
- `skills/` (skill directory)
- `subagents/` (subagent declarations)
- `knowledge/` (knowledge base)
- `.skills-cache/` (skill cache)

Projection compares content by SHA-256; unchanged files skip hydration. Customize which paths are included via `WorkspaceProjectionRoots(List<string>)`, or disable entirely with `WorkspaceProjectionEnabled(false)`.

---

### Mode 3: local + shell (default)

What you get with no `filesystem(...)` call: workspace lives at `${cwd}/.agentscope/workspace/`, shell runs on the host:

```csharp
HarnessAgent agent = HarnessAgent.Builder()
    .Name("local-agent")
    .Model(model)
    .Workspace(workspace)
    // .Filesystem(...) omitted = local + shell
    .Build();
```

#### All configuration options

```csharp
.Filesystem(new LocalFilesystemSpec()
    .ExecuteTimeoutSeconds(120)       // shell command timeout
    .MaxOutputBytes(100_000)          // max output bytes per command
    .Env("MY_VAR", "value")          // extra environment variables
    .InheritEnv(true)                // inherit parent process env
    .Mode(LocalFsMode.ROOTED)        // path policy
    .Project(Path.GetFullPath("/my/project")) // project root (shell cwd + overlay lower)
    .AddRoot(Path.GetFullPath("/extra/dir"))) // extra allowed directory
```

| Method | Description | Default |
|--------|-------------|---------|
| `ExecuteTimeoutSeconds(int)` | Shell command timeout (seconds) | 120 |
| `MaxOutputBytes(int)` | Max captured output bytes per command | 100,000 |
| `Env(string, string)` | Add a shell environment variable | none |
| `InheritEnv(bool)` | Inherit parent process environment | `false` |
| `Mode(LocalFsMode)` | Path resolution policy | `ROOTED` |
| `Project(string)` | Project root directory (overlay lower layer + shell cwd) | `Directory.GetCurrentDirectory()` |
| `AddRoot(string)` | Extra host directory the agent may access | none |
| `AdditionalRoots(ICollection<string>)` | Batch-set extra directories | none |
| `ProjectWritable(bool)` | Route non-workspace writes to the project directory instead of workspace | `false` |

#### Path resolution policy (`LocalFsMode`)

| Mode | Behavior |
|------|----------|
| `ROOTED` (default) | Absolute paths accepted only under `workspace` + `project` + `additionalRoots`; `..` traversal rejected |
| `SANDBOXED` | All paths anchored to the workspace root; absolute paths and `..` both rejected |
| `UNRESTRICTED` | Absolute paths pass through unchanged. Only for tests or fully trusted environments |

#### Overlay filesystem

Local mode actually produces an `OverlayFilesystem`:

- **Upper** (read-write): `LocalFilesystemWithShell`, rooted at `workspace`, provides shell;
- **Lower** (read-only): `LocalFilesystem`, rooted at `project`.

Reads check workspace first, then fall back to project (copy-on-write semantics). Shell `pwd` is the project directory, so `ls` shows project files.

#### Project-writable mode (`ProjectWritable`)

By default all writes land in the workspace — fine for read/analyze scenarios, but if the agent's job is to **generate code** (e.g. scaffold a microservice), files end up in `.agentscope/workspace/` instead of the project directory.

Enable `ProjectWritable(true)` and the framework routes writes by path:

| Path type | Written to | Examples |
|-----------|-----------|----------|
| Workspace metadata | workspace | `MEMORY.md`, `memory/`, `agents/`, `skills/`, `knowledge/`, `plans/`, `subagents/`, `rules/`, `tools.json` |
| Everything else | project directory | `src/main/java/App.java`, `pom.xml`, `README.md`, `docker-compose.yml` |

```csharp
.Filesystem(new LocalFilesystemSpec()
    .ProjectWritable(true)      // code files go to the project directory
    .InheritEnv(true))
```

Read behavior is unchanged — workspace first, project fallback.

#### Example: local development assistant

```csharp
HarnessAgent devHelper = HarnessAgent.Builder()
    .Name("dev-helper")
    .Model(model)
    .Workspace(Path.GetFullPath(".agentscope/workspace"))
    .Filesystem(new LocalFilesystemSpec()
        .Project(Path.GetFullPath("/Users/alice/my-project"))
        .AddRoot(Path.GetFullPath("/Users/alice/.config"))
        .Mode(LocalFsMode.ROOTED)
        .InheritEnv(true)
        .ExecuteTimeoutSeconds(300))
    .Build();
```

The agent can read/write files under `/Users/alice/my-project` and `/Users/alice/.config`, run shell commands with cwd at `/Users/alice/my-project`, but cannot access other host directories.

---

## IsolationScope — bucketing across users and replicas

Both mode 1 (shared store) and mode 2 (sandbox) use the same `IsolationScope` concept to decide **who shares state with whom**:

| Scope | Meaning | Namespace key | Typical use |
|-------|---------|--------------|-------------|
| `SESSION` | Each sessionId is independent | `agents/<agentId>/sessions/<sessionId>/...` | Multi-user SaaS, each conversation fully isolated |
| `USER` (default) | Same `userId` shares across sessions | `agents/<agentId>/users/<userId>/...` | Same user's multiple sessions share long-term memory |
| `AGENT` | All users/sessions of this agent share | `agents/<agentId>/shared/...` | Public-knowledge-base type agent |
| `GLOBAL` | One shared slot for everything | `global/...` | Use with care |

### Fallback rules per scope

- Under `USER` scope, if `RuntimeContext.UserId` is absent, falls back to `SESSION` (isolates by sessionId).
- Under `SESSION` scope, if `RuntimeContext.SessionId` is absent, state lookup is skipped and a fresh environment is created.
- `AGENT` scope uses the agent name (fixed at build time) as the namespace key — it never degrades due to missing context fields.

### Concurrency in sandbox mode

`IsolationScope` in sandbox mode is **sequential-reuse** sharing, not live-instance sharing. Concurrent calls at the same scope key each get their own running container; at call end, the last-written snapshot wins. For `AGENT` / `GLOBAL` scopes where multiple users share state, use `ExecutionGuard(SandboxExecutionGuard)` to serialize concurrent access.

### Example: scope combinations for different business needs

**Scenario 1: per-user coding sandbox, preserving installed deps across sessions**

```csharp
.Filesystem(new DockerFilesystemSpec()
    .Image("python:3.12")
    .IsolationScope(IsolationScope.USER)       // all of Alice's sessions share one snapshot
    .SnapshotSpec(new LocalSnapshotSpec("/snapshots")))
```

**Scenario 2: per-conversation disposable sandbox**

```csharp
.Filesystem(new DockerFilesystemSpec()
    .Image("ubuntu:24.04")
    .IsolationScope(IsolationScope.SESSION))   // each sessionId independent
```

**Scenario 3: shared-knowledge customer-service agent (shared store)**

```csharp
.DistributedStore(store)
    .Filesystem(new RemoteFilesystemSpec()
    .IsolationScope(IsolationScope.AGENT))     // all users and sessions share memory / skills
```

---

## How multi-user isolation works

`RuntimeContext.UserId` is the key to multi-user splitting:

| Mode | What userId does | Physical manifestation |
|------|-----------------|----------------------|
| Local | User-level files land in `workspace/<userId>/...`, e.g. `workspace/alice/skills/code-reviewer/SKILL.md` only applies to Alice | path prefix |
| Shared store | Used as KV namespace prefix `agents/<agentId>/users/<userId>/...` | KV key prefix |
| Sandbox | Used as sandbox snapshot slot key (paired with `IsolationScope.USER`) | sandbox instance isolation |

Without `userId`, single-tenant default applies and everyone shares one root.

### Runtime data vs static assets

**Runtime data** (conversation logs, tasks, memory) follows `IsolationScope` / `userId` and is automatically isolated.

**Static assets** (`AGENTS.md`, `tools.json`, `knowledge/`) are shared across all users and are **not** auto-partitioned by userId. Differentiation is only possible through per-user override directories:

```
workspace/
├── skills/code-reviewer/SKILL.md     ← shared (visible to everyone)
└── alice/
    └── skills/code-reviewer/SKILL.md ← only applies to Alice; overrides shared
```

---

## How skills and tools behave in each mode

### Skills

`DynamicSkillMiddleware` merges skills from the repository list before each reasoning turn and renders them into the system prompt. Skill file loading goes through the `AbstractFilesystem` interface, so it works transparently across all three modes:

| Mode | How skills load |
|------|----------------|
| Local | Read directly from `workspace/skills/` on local disk; `<userId>/skills/` for per-user overrides |
| Shared store | `skills/` routes to KV — checks remote first, falls back to local template. Admin edits take effect on the next reasoning turn across all replicas |
| Sandbox | Host `skills/` are injected into the sandbox's `/workspace/skills/` via workspace projection at startup |

The four-layer priority is unchanged (low → high): `projectGlobalSkillsDir` → `skillRepository` → `workspace/skills/` → `<userId>/skills/`.

### File tools (read_file / write_file / edit_file / ...)

All file tools call through the `AbstractFilesystem` interface, passing the current `RuntimeContext` on every operation. The filesystem implementation decides the actual read/write location. Agent code is completely unaware of the mode.

| Mode | Read/write behavior |
|------|-------------------|
| Local | `OverlayFilesystem`: writes land in workspace (upper); reads check workspace first, then project (lower). With `ProjectWritable(true)`, non-metadata writes are routed to the project directory |
| Shared store | `CompositeFilesystem`: routed paths go through KV overlay (remote upper + local template lower); others go local |
| Sandbox | All file operations forwarded into the sandbox container |

### Shell execution (execute)

| Mode | Shell available? | Where it runs |
|------|-----------------|--------------|
| Local | Yes | Host `sh -c`, cwd = `project` directory |
| Shared store | No | Shell not provided |
| Sandbox | Yes | Inside the sandbox container |

### tools.json / MCP servers

`tools.json` is read once from the workspace at `Build()` time (through `WorkspaceManager`, supporting two-layer reads), registering MCP servers and applying allow/deny filters. **Behavior is the same across all three modes** — configuration is read at build time, unaffected by the runtime filesystem mode.

Under shared-store mode, `tools.json` also follows the "remote upper, local template lower" overlay: modifying `tools.json` via an admin console requires **re-building the agent to take effect** (MCP server registration is a one-time operation).

---

## Two-layer reading in the workspace

Key files like `AGENTS.md`, `MEMORY.md`, `KNOWLEDGE.md` have a "two-layer fallback" on reads: look in your configured filesystem first, fall back to local disk if not found. This is useful for **"template files" in mode 1 (shared store)**: the first replica's local has the template `AGENTS.md` so it works immediately; later replicas read the up-to-date version from the shared store.

Writes always go through the configured filesystem store.

## Fully self-managed: `abstractFilesystem(...)`

If none of the three modes fits, pass a fully self-implemented filesystem:

```csharp
HarnessAgent.Builder()
    ...
    .AbstractFilesystem(myCustomFilesystem)   // mutually exclusive with filesystem(...)
    .Build();
```

Usually not needed — the three modes cover ~95% of use cases.

## Related Pages

- [Sandbox](./sandbox.md) — runtime details of mode 2 (container lifecycle, snapshot recovery chain)
- [Workspace](./workspace.md) — directory layout, loading mechanics, the "lower layer" of two-layer reads
- [Context](../building-blocks/context.md) — `AgentState` and `AgentStateStore`, `(userId, sessionId)` addressing
- [Skills](./skill.md) — four-layer composition, self-learning loop, the `<available_skills>` block
- [Tools](../building-blocks/tool.md) — `read_file` / `write_file` / `execute` parameters
- [Architecture](./architecture.md) — how filesystem and runtime context cooperate
