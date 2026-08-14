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
