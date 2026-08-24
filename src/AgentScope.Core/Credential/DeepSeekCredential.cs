// Copyright 2024-2026 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using AgentScope.Core.Model.DeepSeek;

namespace AgentScope.Core.Credential;

/// <summary>
/// DeepSeek 凭据。
/// </summary>
public class DeepSeekCredential : CredentialBase
{
    public DeepSeekCredential(string apiKey, string? baseUrl = null)
        : base("deepseek", apiKey, baseUrl)
    {
    }

    public override Type GetChatModelClass() => typeof(DeepSeekModel);

    public override List<ModelCard> ListModels()
    {
        return new List<ModelCard>
        {
            new()
            {
                ModelId = "deepseek-chat",
                DisplayName = "DeepSeek Chat",
                ProviderId = Id,
                ContextWindow = 65536,
                Description = "DeepSeek-V2 / V3 系列对话模型",
                SupportsStreaming = true,
                SupportsToolUse = true,
            },
            new()
            {
                ModelId = "deepseek-reasoner",
                DisplayName = "DeepSeek Reasoner",
                ProviderId = Id,
                ContextWindow = 65536,
                Description = "DeepSeek-R1 推理模型",
                SupportsStreaming = true,
                SupportsToolUse = false,
            },
        };
    }
}
