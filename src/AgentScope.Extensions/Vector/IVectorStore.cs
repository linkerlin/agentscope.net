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

namespace AgentScope.Extensions.Vector;

/// <summary>
/// 向量存储接口。对标 Java VDBStoreBase。
/// 子工程（Qdrant/Milvus/PgVector/ES）通过此接口接入。
/// </summary>
public interface IVectorStore : IAsyncDisposable
{
    int Dimension { get; }
    ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default);
    IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, CancellationToken ct = default);
}
