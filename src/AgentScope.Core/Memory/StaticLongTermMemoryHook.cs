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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgentScope.Core.Memory;

/// <summary>
/// Wraps an <see cref="ILongTermMemory"/> and produces a formatted context
/// string suitable for injection into prompts.
/// </summary>
public class StaticLongTermMemoryHook
{
    private readonly ILongTermMemory _memory;

    public StaticLongTermMemoryHook(ILongTermMemory memory) => _memory = memory;

    /// <summary>
    /// Retrieves relevant memories for the given query and returns a
    /// prompt-ready context string (empty when there are no matches).
    /// </summary>
    public async Task<string> GetContextAsync(string query, int topK = 5)
    {
        var facts = await _memory.SearchAsync(query, topK);
        return facts.Count == 0 ? "" : "Relevant long-term memory:\n" + string.Join("\n", facts.Select(f => "- " + f));
    }

    public async Task AddAsync(string text, Dictionary<string, object>? metadata = null) =>
        await _memory.AddAsync(text, metadata);
}
