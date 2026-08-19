# MiniMax Model

`AgentScope.Extensions.Model.OpenAI` provides first-class MiniMax support through the OpenAI-compatible model stack. Add the OpenAI model extension module, then use `minimax:<model>` with `ModelRegistry`.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Model.OpenAI" Version="$(AgentScopeVersion)" />
```

## ModelRegistry

Set `MINIMAX_API_KEY`, then use the `minimax:<model>` id:

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("minimax:MiniMax-M3") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

The provider base URL defaults to `https://api.minimaxi.com/v1`. `OpenAIClient` appends the default chat completions endpoint, so the final request URL is `https://api.minimaxi.com/v1/chat/completions`, matching the MiniMax OpenAI-compatible API. The provider strips the `minimax:` prefix before sending the model name and uses the MiniMax formatter from `AgentScope.Extensions.Model.OpenAI.Compat.MiniMax`.

## Thinking mode

Pass MiniMax thinking options through `ModelCreationContext` when resolving the model:

```csharp
using AgentScope.Core.Model;

Model model = ModelRegistry.Resolve(
    "minimax:MiniMax-M3",
    ModelCreationContext.Builder()
        .EnableThinking(false)
        .Build());
```

`enableThinking(false)` sends `thinking: {"type": "disabled"}` and `enableThinking(true)` sends `thinking: {"type": "adaptive"}`. MiniMax-M3 uses adaptive thinking by default when `thinking` is omitted; M2.x models keep thinking enabled even when disabled is requested. The formatter enables `reasoning_split` by default so MiniMax thinking content can be parsed as `ThinkingBlock`.

## Compatibility notes

The MiniMax formatter adapts OpenAI-style requests to the MiniMax OpenAI-compatible Chat Completions API. It maps `max_tokens` to `max_completion_tokens`, because MiniMax marks `max_tokens` as deprecated.

MiniMax tool definitions support function tools, but the official schema does not include the tool schema `strict` field, so the default formatter omits `strict` even when a tool is registered with strict schema validation. MiniMax also does not document `tool_choice`, so explicit tool-choice settings are removed from MiniMax requests.

The formatter removes unsupported OpenAI-only request fields such as `reasoning_effort`, `frequency_penalty`, `presence_penalty`, `thinking_budget`, `parallel_tool_calls`, `response_format`, and `seed`. Structured output uses the normal AgentScope fallback behavior by default because MiniMax does not document OpenAI `response_format` support for schema-constrained output.

For compatible or self-hosted endpoints, pass `baseUrl`, `endpointPath`, generation options, or formatter overrides through `ModelCreationContext`.
