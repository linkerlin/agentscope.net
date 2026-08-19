# Kimi Model

`AgentScope.Extensions.Model.OpenAI` provides first-class Kimi (Moonshot AI) support through the OpenAI-compatible model stack. Add the OpenAI model extension module, then use `kimi:<model>` with `ModelRegistry`.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Model.OpenAI" Version="$(AgentScopeVersion)" />
```

## ModelRegistry

Set `MOONSHOT_API_KEY` or `KIMI_API_KEY`, then use the `kimi:<model>` id:

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("kimi:kimi-k3") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

The provider defaults to `https://api.moonshot.cn/v1`, strips the `kimi:` prefix before sending the model name, and uses the Kimi formatter from `AgentScope.Extensions.Model.OpenAI.Compat.Kimi`.

## Thinking mode

Pass Kimi thinking options through `GenerateOptions` when resolving the model:

```csharp
using AgentScope.Core.Model;
using System.Collections.Generic;

Model model = ModelRegistry.Resolve(
    "kimi:kimi-k2.6",
    ModelCreationContext.Builder()
        .Component(
            typeof(GenerateOptions),
            GenerateOptions.Builder()
                .AdditionalBodyParam("thinking", new Dictionary<string, string> { { "type", "disabled" } })
                .MaxCompletionTokens(16000)
                .Build())
        .Build());
```

`kimi-k3` uses the top-level `reasoning_effort` option (`low`, `high`, or `max`). `kimi-k3` and `kimi-k2.7-code` always run with thinking enabled. `kimi-k2.6` and `kimi-k2.5` enable thinking by default, but can disable it with `additionalBodyParam("thinking", Map.of("type", "disabled"))`.

## Compatibility notes

The Kimi formatter adapts OpenAI-style requests to the Kimi chat-completions API. It omits tool schema `strict`, preserves assistant `reasoning_content` in message history, and strips unsupported request fields such as `thinking_budget`.

On `kimi-*` models, sampling parameters such as `temperature`, `top_p`, `n`, `frequency_penalty`, and `presence_penalty` are fixed by the platform and are removed from requests. The `moonshot-v1` series keeps those parameters. Kimi documents `max_completion_tokens`, so `max_tokens` is mapped to `max_completion_tokens` when `max_completion_tokens` is not already set.

`reasoning_effort` is kept only for `kimi-k3`. For K2.x thinking controls, pass the `thinking` body parameter through `GenerateOptions.AdditionalBodyParam`.

`tool_choice=auto` and `tool_choice=none` are supported broadly. `tool_choice=required` is degraded to `auto` on K2.x models. Forcing a specific function is incompatible with thinking enabled, so it is degraded to `auto` on `kimi-k3`, on `kimi-k2.7-code`, and on `kimi-k2.6` / `kimi-k2.5` unless `thinking.type` is explicitly set to `disabled`.

Structured output uses the normal AgentScope fallback behavior by default.

For compatible or self-hosted endpoints, pass `baseUrl`, `endpointPath`, generation options, or formatter overrides through `ModelCreationContext`.
