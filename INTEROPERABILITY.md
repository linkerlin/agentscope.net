# AgentScope.NET and AgentScope-Java Interoperability

## Overview

This document describes how AgentScope.NET and agentscope-java can interoperate, allowing agents written in C# and Java to communicate with each other.

## Message Protocol

Both implementations use JSON for message serialization, ensuring compatibility.

### Message Structure

```json
{
  "id": "unique-guid",
  "name": "AgentName",
  "role": "user|assistant|system",
  "content": "Message content or structured object",
  "url": ["optional", "list", "of", "urls"],
  "timestamp": "2026-02-17T17:00:00Z",
  "metadata": {
    "key1": "value1",
    "key2": "value2"
  }
}
```

### Message Mapping

| Java (agentscope-java) | C# (AgentScope.NET) | Type |
|------------------------|---------------------|------|
| `Msg` | `Msg` | Message class |
| `String id` | `string Id` | Message ID |
| `String name` | `string? Name` | Agent name |
| `MsgRole role` | `string Role` | Message role |
| `Object content` | `object? Content` | Message content |
| `List<String> url` | `List<string>? Url` | URLs |
| `Instant timestamp` | `DateTime Timestamp` | Timestamp |
| `Map<String, Object> metadata` | `Dictionary<string, object>? Metadata` | Metadata |

## Serialization Compatibility

### C# to Java

```csharp
// C# - AgentScope.NET
var msg = Msg.Builder()
    .Name("CSharpAgent")
    .Role("assistant")
    .TextContent("Hello from C#")
    .AddMetadata("lang", "csharp")
    .Build();

var json = msg.ToString(); // Uses Newtonsoft.Json
// Send via HTTP, WebSocket, or message queue
```

### Java to C#

```java
// Java - agentscope-java
Msg msg = Msg.builder()
    .name("JavaAgent")
    .role(MsgRole.ASSISTANT)
    .textContent("Hello from Java")
    .metadata(Map.of("lang", "java"))
    .build();

String json = objectMapper.writeValueAsString(msg);
// Send via HTTP, WebSocket, or message queue
```

## Communication Channels

### 1. HTTP REST API (Recommended)

Both implementations can expose REST APIs for message exchange.

**C# Implementation:**
```csharp
// ASP.NET Core Web API
[ApiController]
[Route("api/agent")]
public class AgentController : ControllerBase
{
    private readonly ReActAgent _agent;

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] Msg message)
    {
        var response = await _agent.CallAsync(message);
        return Ok(response);
    }
}
```

**Java Implementation:**
```java
// Spring Boot REST API
@RestController
@RequestMapping("/api/agent")
public class AgentController {
    private final ReActAgent agent;

    @PostMapping("/message")
    public Mono<Msg> sendMessage(@RequestBody Msg message) {
        return agent.call(message);
    }
}
```

### 2. Message Queues (RabbitMQ, Kafka)

For asynchronous communication:

**C# Producer:**
```csharp
var msg = Msg.Builder().TextContent("Hello").Build();
var json = JsonConvert.SerializeObject(msg);
await producer.SendAsync("agent.messages", json);
```

**Java Consumer:**
```java
@RabbitListener(queues = "agent.messages")
public void handleMessage(String json) {
    Msg msg = objectMapper.readValue(json, Msg.class);
    agent.call(msg).subscribe();
}
```

### 3. gRPC (For High Performance)

Define shared `.proto` files for strong typing.

## Database Compatibility

Both implementations use SQLite for persistent memory, ensuring compatibility at the storage level.

### Schema Compatibility

| Java Column | C# Column | Type | Notes |
|-------------|-----------|------|-------|
| `id` | `Id` | INTEGER PRIMARY KEY | Auto-increment |
| `message_id` | `MessageId` | TEXT | GUID/UUID |
| `name` | `Name` | TEXT | Agent name |
| `role` | `Role` | TEXT | Message role |
| `content` | `Content` | TEXT | Message content |
| `timestamp` | `Timestamp` | TEXT | ISO 8601 format |
| `metadata` | `Metadata` | TEXT | JSON blob |
| `url` | `Url` | TEXT | JSON array |

### Shared Database

Both implementations can share the same SQLite database:

```csharp
// C# - Read from shared database
var memory = new SqliteMemory("shared-agentscope.db");
var messages = memory.GetAll(); // Works with Java-created messages
```

```java
// Java - Read from shared database
var memory = new SqliteMemory("shared-agentscope.db");
var messages = memory.getAll(); // Works with C#-created messages
```

## Configuration Compatibility

Both implementations support `.env` files:

```bash
# .env - Shared configuration
OPENAI_API_KEY=sk-...
DATABASE_PATH=shared-agentscope.db
MAX_ITERATIONS=10
DEFAULT_MODEL=gpt-3.5-turbo
```

**C# Loading:**
```csharp
ConfigurationManager.Load();
var apiKey = ConfigurationManager.GetOpenAIApiKey();
```

**Java Loading:**
```java
Dotenv dotenv = Dotenv.load();
String apiKey = dotenv.get("OPENAI_API_KEY");
```

## Tool/Skill Interoperability

Tools can be exposed as REST endpoints:

**C# Tool Service:**
```csharp
[ApiController]
[Route("api/tools")]
public class ToolController : ControllerBase
{
    [HttpPost("calculator")]
    public async Task<ToolResult> Calculate([FromBody] Dictionary<string, object> parameters)
    {
        var tool = new CalculatorTool();
        return await tool.ExecuteAsync(parameters);
    }
}
```

**Java Tool Consumer:**
```java
public Mono<ToolResult> executeRemoteTool(String toolUrl, Map<String, Object> parameters) {
    return webClient.post()
        .uri(toolUrl)
        .bodyValue(parameters)
        .retrieve()
        .bodyToMono(ToolResult.class);
}
```

## Multi-Agent Scenarios

### Scenario 1: Java Coordinator, C# Workers

```
Java Coordinator Agent
    ↓ (HTTP POST)
C# Worker Agent 1 → SQLite Database
C# Worker Agent 2 → SQLite Database
    ↓ (HTTP Response)
Java Coordinator Agent
```

### Scenario 2: Mixed Agent Conversation

```
C# Agent A → Message Queue → Java Agent B
    ↓                           ↓
SQLite Database ← Shared ← SQLite Database
```

## Best Practices

1. **Use JSON for All Messages**: Ensures maximum compatibility
2. **ISO 8601 for Timestamps**: Both platforms support this format
3. **UTF-8 Encoding**: For all text content
4. **Shared SQLite Database**: For persistent memory across implementations
5. **REST APIs**: For synchronous request/response
6. **Message Queues**: For asynchronous workflows
7. **Consistent Environment Variables**: Use the same `.env` structure

## Testing Interoperability

Create integration tests that verify cross-platform communication:

```csharp
[Fact]
public async Task CSharpAgent_CanReadJavaMessages()
{
    // Arrange - Java creates messages in database
    await SimulateJavaAgentCreatingMessages();
    
    // Act - C# reads messages
    using var memory = new SqliteMemory("interop-test.db");
    var messages = memory.GetAll();
    
    // Assert
    Assert.NotEmpty(messages);
    Assert.All(messages, msg => Assert.NotNull(msg.Id));
}
```

## Example: Cross-Platform Chat

### Java Service
```java
@RestController
public class JavaAgentController {
    @PostMapping("/chat")
    public Mono<Msg> chat(@RequestBody Msg message) {
        return agent.call(message);
    }
}
```

### C# Client
```csharp
public async Task<Msg> ChatWithJavaAgent(string text)
{
    var msg = Msg.Builder().TextContent(text).Build();
    var json = JsonConvert.SerializeObject(msg);
    
    var response = await httpClient.PostAsync(
        "http://java-agent:8080/chat",
        new StringContent(json, Encoding.UTF8, "application/json")
    );
    
    var responseJson = await response.Content.ReadAsStringAsync();
    return JsonConvert.DeserializeObject<Msg>(responseJson);
}
```

## Conclusion

AgentScope.NET and agentscope-java are designed with interoperability in mind:
- ✅ Compatible JSON serialization
- ✅ Shared SQLite database schema
- ✅ Common .env configuration format
- ✅ REST API compatibility
- ✅ Message queue support
- ✅ Tool/skill service architecture

This allows building heterogeneous agent systems where some agents run on the JVM and others on .NET, communicating seamlessly.
