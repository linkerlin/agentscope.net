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

namespace AgentScope.Extensions.Store;

/// <summary>
/// 分布式存储接口。对标 Java BaseStore。
/// </summary>
public interface IDistributedStore
{
    ValueTask<string?> GetAsync(string key, CancellationToken ct = default);
    ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default);
    ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default);
    IAsyncEnumerable<string> ListKeysAsync(string prefix, CancellationToken ct = default);
}
