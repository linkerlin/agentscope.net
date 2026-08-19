# GLM Model

`AgentScope.Extensions.Model.OpenAI` provides first-class GLM (Zhipu AI / Z.AI) support through the OpenAI-compatible model stack. Add the OpenAI model extension module, then use `glm:<model>` with `ModelRegistry`.

## Add the dependency

```xml
<PackageReference Include="AgentScope.Extensions.Model.OpenAI" Version="$(AgentScopeVersion)" />
```

## ModelRegistry

Set `ZAI_API_KEY`, `GLM_API_KEY`, or `ZHIPUAI_API_KEY`, then use the `glm:<model>` id:

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("glm:glm-5.2") // Resolved internally by ModelRegistry.Resolve(modelId)
    .Build();
```

The provider defaults to `https://open.bigmodel.cn/api/paas/v4`, strips the `glm:` prefix before sending the model name, and uses the GLM formatter from `AgentScope.Extensions.Model.OpenAI.Compat.Glm`.

## Thinking mode

Pass GLM thinking options through `GenerateOptions` when resolving the model:

```csharp
using AgentScope.Core.Model;
using System.Collections.Generic;

Model model = ModelRegistry.Resolve(
    "glm:glm-5.2",
    ModelCreationContext.Builder()
        .Component(
            typeof(GenerateOptions),
            GenerateOptions.Builder()
                .AdditionalBodyParam("thinking", new Dictionary<string, string> { { "type", "disabled" } })
                .ReasoningEffort("max")
                .Build())
        .Build());
```

GLM-4.7 and GLM-5 series enable thinking by default. GLM-5.2 also supports `reasoning_effort`, and streaming tool-call arguments can be enabled with `additionalBodyParam("tool_stream", true)`.

## Compatibility notes

The GLM formatter adapts OpenAI-style requests to the GLM chat-completions API. It ensures at least one user message exists, removes unsupported message `name` fields, omits tool schema `strict`, and strips unsupported request fields such as `frequency_penalty`, `presence_penalty`, `thinking_budget`, and `stream_options`.

GLM only supports `max_tokens`, so `max_completion_tokens` is mapped to `max_tokens` when `max_tokens` is not already set. `temperature` and `top_p` are clamped to the GLM-supported ranges.

GLM only accepts `tool_choice=auto`. Forced choices are degraded to `auto`; `ToolChoice.None` removes tools from the request to preserve the no-tool-call contract.

Structured output uses the normal AgentScope fallback behavior by default because GLM `response_format` only supports `json_object`. Only enable native structured output when you have confirmed the target endpoint supports the behavior you need.

For compatible or self-hosted endpoints, pass `baseUrl`, `endpointPath`, generation options, or formatter overrides through `ModelCreationContext`.
