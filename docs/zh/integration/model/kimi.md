# Kimi 模型

`AgentScope.Extensions.Model.OpenAI` 通过 OpenAI 兼容模型栈提供 Kimi（月之暗面 / Moonshot AI）的一等支持。引入 OpenAI 模型扩展模块后，可以通过 `ModelRegistry` 使用 `kimi:<model>`。

## 添加依赖

```xml
<PackageReference Include="AgentScope.Extensions.Model.OpenAI" Version="*" />
```

## ModelRegistry

设置 `MOONSHOT_API_KEY` 或 `KIMI_API_KEY` 后，使用 `kimi:<model>` 字符串 id：

```csharp
ReActAgent agent = ReActAgent.Builder()
    .Name("assistant")
    .Model("kimi:kimi-k3") // 底层由 ModelRegistry.Resolve(modelId) 解析
    .Build();
```

Provider 默认使用 `https://api.moonshot.cn/v1` ，发送请求前会去掉 `kimi:` 前缀，并使用 `AgentScope.Extensions.Model.OpenAI.Compat.Kimi` 下的 Kimi formatter。

## 思考模式

通过 `ModelCreationContext` 解析模型时，可以用 `GenerateOptions` 传入 Kimi 思考相关参数：

```csharp
using AgentScope.Core.Model;
using System.Collections.Generic;

Model model = ModelRegistry.Resolve(
    "kimi:kimi-k2.6",
    ModelCreationContext.Builder()
        .Component(
            typeof(GenerateOptions),
            GenerateOptions.Builder()
                .AdditionalBodyParam("thinking", new Dictionary<string, object> { ["type"] = "disabled" })
                .MaxCompletionTokens(16000)
                .Build())
        .Build());
```

`kimi-k3` 使用顶层 `reasoning_effort` 选项（`low`、`high` 或 `max`）。`kimi-k3` 和 `kimi-k2.7-code` 始终开启思考模式；`kimi-k2.6` 和 `kimi-k2.5` 默认开启思考模式，但可以通过 `AdditionalBodyParam("thinking", new Dictionary<string, object> { ["type"] = "disabled" })` 关闭。

## 兼容性说明

Kimi formatter 会把 OpenAI 风格请求适配为 Kimi chat-completions API 支持的格式。它会省略工具 schema 的 `strict` 字段，保留历史 assistant 消息中的 `reasoning_content`，并移除 `thinking_budget` 等不支持的请求字段。

在 `kimi-*` 模型上，`temperature`、`top_p`、`n`、`frequency_penalty`、`presence_penalty` 等采样参数由平台固定，formatter 会从请求中移除这些字段。`moonshot-v1` 系列仍会保留这些参数。Kimi 文档使用 `max_completion_tokens`，因此当只设置了 `max_tokens` 且没有设置 `max_completion_tokens` 时，会自动映射到 `max_completion_tokens`。

`reasoning_effort` 只会在 `kimi-k3` 上保留。K2.x 的思考控制参数需要通过 `GenerateOptions.AdditionalBodyParam` 传入 `thinking` body 参数。

`tool_choice=auto` 和 `tool_choice=none` 通常可以直接使用。K2.x 模型不支持 `tool_choice=required`，formatter 会将其降级为 `auto`。tool_choice 指定函数调用时与开启思考模式不兼容，因此在 `kimi-k3`、`kimi-k2.7-code`，以及未显式设置 `thinking.type=disabled` 的 `kimi-k2.6` / `kimi-k2.5` 上会降级为 `auto`。

结构化输出默认使用 AgentScope 的 fallback 行为。

使用兼容端点或自托管端点时，可以通过 `ModelCreationContext` 传入 `baseUrl`、`endpointPath`、生成参数或 formatter 覆盖。
