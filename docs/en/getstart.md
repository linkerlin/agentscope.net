# AgentScope .NET Get Started

## 1. Model Configuration

```csharp
using AgentScope.Core.Model.OpenAI;

IModel model = new OpenAIModel(
    "model-name",
    "API Key or none (for private deployment)",
    "API Base URL (private endpoint address)");
```

**Supported Model Providers:**

| Provider | Model Class | Notes |
|----------|-------------|-------|
| OpenAI Compatible | `OpenAIModel` | Official API, vllm, Ollama endpoints |
| DashScope | `DashScopeModel` | Alibaba Cloud Qwen |
| Anthropic | `AnthropicModel` | Claude series |
| Gemini | `GeminiModel` | Google Gemini |
| Mock | `MockModel` | Local testing without real API key |

---

## 2. Building HarnessAgent

```csharp
using AgentScope.Harness;
using AgentScope.Harness.Middleware;

HarnessAgent agent = new HarnessAgentBuilder()
    .WithName("agent-name")
    .WithSystemPrompt("You are a helpful assistant.")
    .WithModel(model)
    .WithWorkspaceRoot(Path.GetFullPath(".agentscope/workspace"))
    .WithMiddleware(new CompactionMiddleware(maxContextLength: 4096))
    .Build();
```

**Builder Methods:**

| Method | Description |
|--------|-------------|
| `WithName(name)` | Agent name |
| `WithSystemPrompt(prompt)` | System prompt |
| `WithModel(model)` | Model instance |
| `WithWorkspaceRoot(path)` | Workspace path (for state persistence) |
| `WithMiddleware(mw)` | Add middleware (e.g., CompactionMiddleware) |

---

## 3. Runtime Context

```csharp
RuntimeContext ctx = RuntimeContext.Empty
    .WithUserId("alice")
    .WithSessionId("demo-session");
```

**Same (userId, sessionId) recovers conversation state across calls.**

---

## 4. Non-Streaming Call: CallAsync

Returns the final reply as a `Msg` in one shot:

```csharp
// Build user message
Msg userMsg = Msg.Builder()
    .Role("user")
    .TextContent("Hello, I'm Alice. Today I'll prepare a tech talk about ReAct.")
    .Build();

// Call agent (non-streaming)
Msg reply = await agent.CallAsync(userMsg, ctx);
Console.WriteLine($"Assistant: {reply.GetTextContent()}");
```

**Use Case:** Only need the final result, don't care about intermediate process.

---

## 5. Streaming Call: StreamEventsAsync

`StreamEventsAsync` yields events step by step, showing the reasoning/acting/summary process in real-time:

```csharp
await foreach (var ev in agent.StreamEventsAsync(userMsg, ctx))
{
    // ev.Type  — event type
    // ev.Message?.GetTextContent() — text content of the event
    // ev.IsLast — whether this is the last event

    switch (ev.Type)
    {
        case EventType.ReasoningChunk:
            // Incremental text from the model's reasoning
            Console.Write(ev.Message?.GetTextContent());
            break;

        case EventType.ReasoningStart:
            Console.WriteLine("\n[Reasoning Start]");
            break;

        case EventType.ReasoningFinish:
            Console.WriteLine("\n[Reasoning End]");
            break;

        case EventType.SummaryChunk:
            // Incremental summary text
            Console.Write(ev.Message?.GetTextContent());
            break;

        case EventType.ActingChunk:
            // Tool execution result
            Console.WriteLine($"[Tool Result] {ev.Message?.GetTextContent()}");
            break;

        case EventType.Error:
            Console.WriteLine($"[Error] {ev.Message?.GetTextContent()}");
            break;
    }
}
```

### Event Types

| Event Type | Description | Has Text |
|-----------|-------------|----------|
| `ReasoningStart` | Reasoning started | ❌ |
| `ReasoningChunk` | Reasoning incremental text | ✅ |
| `ReasoningFinish` | Reasoning ended | ❌ |
| `ActingStart` | Acting started | ❌ |
| `ActingChunk` | Tool execution result | ✅ |
| `ActingFinish` | Acting ended | ❌ |
| `SummaryStart` | Summary started | ❌ |
| `SummaryChunk` | Summary incremental text | ✅ |
| `SummaryFinish` | Summary ended | ❌ |
| `Error` | Error occurred | ✅ |

---

## 6. Multi-Turn Conversation with Memory

Same `(userId, sessionId)` across multiple `CallAsync` / `StreamEventsAsync` calls automatically restores previous conversation state:

```csharp
RuntimeContext ctx = RuntimeContext.Empty
    .WithUserId("alice")
    .WithSessionId("demo-session");

// Round 1
Msg first = Msg.Builder().Role("user").TextContent("I'm Alice. I'll prepare a tech talk about ReAct.").Build();
var reply1 = await agent.CallAsync(first, ctx);
Console.WriteLine($"Round 1: {reply1.GetTextContent()}");

// Round 2 — same sessionId, remembers my name and task
Msg second = Msg.Builder().Role("user").TextContent("What's my name? What will I do today?").Build();
var reply2 = await agent.CallAsync(second, ctx);
Console.WriteLine($"Round 2: {reply2.GetTextContent()}");
// Output: Your name is Alice. You'll prepare a tech talk about ReAct today.
```

---

## 7. Full Examples

See `examples/AgentScope.Lab/GetStarted.cs` and `examples/AgentScope.Lab/Program.cs` for complete runnable examples.
