using System.Runtime.CompilerServices;
using AgentScope.Extensions.Vector;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace AgentScope.Extensions.Vector.Elasticsearch;

/// <summary>
/// Elasticsearch 向量存储适配器。对标 Java ElasticsearchStore。
/// </summary>
public sealed class ElasticsearchStore(ElasticsearchClient client, string indexName, int dimension) : IVectorStore
{
    public int Dimension => dimension;

    public async ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default)
    {
        var idx = collection ?? indexName;
        var doc = new Dictionary<string, object> { ["vector"] = vector, ["id"] = id };
        if (payload != null)
            foreach (var kv in payload) doc[kv.Key] = kv.Value;

        await client.IndexAsync(doc, idx, id, ct);
    }

    public async IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var idx = collection ?? indexName;
        var response = await client.SearchAsync<Dictionary<string, object>>(s => s
            .Index(idx)
            .Size(topK)
            .Query(q => q
                .Knn(k => k
                    .Field("vector")
                    .QueryVector(query)
                    .k(topK)
                    .NumCandidates(topK * 10))));

        foreach (var hit in response.Hits)
        {
            ct.ThrowIfCancellationRequested();
            yield return new SearchHit(
                hit.Id ?? hit.Source?.GetValueOrDefault("id")?.ToString() ?? "",
                (float)(hit.Score ?? 0));
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
