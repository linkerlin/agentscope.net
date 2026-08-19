# DeepSeek Model

`AgentScope.Extensions.Model.OpenAI` provides first-class DeepSeek support through the OpenAI-compatible model stack. Add the OpenAI model extension module, then use `deepseek:<model>` with `ModelRegistry`.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Model.OpenAI" Version="$(AgentScopeVersion)" />
```

## ModelRegistry

Set `DEEPSEEK_API_KEY`, then use the `deepseek:<model>` id:

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("deepseek:deepseek-v4-flash") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

The provider defaults to `https://api.deepseek.com`, strips the `deepseek:` prefix before sending the model name, and uses the DeepSeek formatter from `AgentScope.Extensions.Model.OpenAI.Compat.DeepSeek`.

## Thinking mode

Enable DeepSeek thinking mode through `ModelCreationContext` when resolving the model:

```csharp
using AgentScope.Core.Model;

Model model = ModelRegistry.Resolve(
    "deepseek:deepseek-v4-flash",
    ModelCreationContext.Builder()
        .EnableThinking(true)
        .Build());
```

Streaming callers can render `ThinkingBlockDeltaEvent` separately from `TextBlockDeltaEvent`.

## Compatibility notes

The DeepSeek formatter preserves DeepSeek-compatible message fields, including `system` roles and supported `name` fields. It also removes stale reasoning content from previous turns while preserving reasoning content needed by current tool-call context.

DeepSeek's stable endpoint does not use the tool schema `strict` field by default, so the default formatter omits `strict` even when a tool is registered with strict schema validation. Structured output uses the normal AgentScope fallback behavior unless you explicitly configure native structured output for a compatible endpoint.

For beta or compatible endpoints, pass `baseUrl`, `endpointPath`, generation options, or formatter overrides through `ModelCreationContext`.
