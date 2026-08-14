using System.Runtime.CompilerServices;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace AgentScope.Extensions.Vector.Qdrant;

/// <summary>
/// Qdrant 向量存储适配器。对标 Java QdrantStore。
/// 子工程隔离 Qdrant.Client 重依赖。
/// </summary>
public sealed class QdrantStore(QdrantClient client, int dimension) : IVectorStore
{
    public int Dimension => dimension;

    public async ValueTask UpsertAsync(string collection, string id, float[] vector,
        IDictionary<string, object>? payload = null, CancellationToken ct = default)
    {
        var point = new PointStruct
        {
            Id = ulong.Parse(id),
            Vectors = vector
        };

        if (payload != null)
        {
            foreach (var kv in payload)
            {
                var qValue = new Value();
                if (kv.Value is string s) qValue.StringValue = s;
                else if (kv.Value is long l) qValue.IntegerValue = l;
                else if (kv.Value is double d) qValue.DoubleValue = d;
                else if (kv.Value is bool b) qValue.BoolValue = b;
                else qValue.StringValue = kv.Value?.ToString() ?? "";
                point.Payload.Add(kv.Key, qValue);
            }
        }

        await client.UpsertAsync(collection, [point], cancellationToken: ct);
    }

    public async IAsyncEnumerable<SearchHit> SearchAsync(string collection, float[] query,
        int topK = 5, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var results = await client.SearchAsync(collection, query, limit: (ulong)topK, cancellationToken: ct);
        foreach (var p in results)
        {
            var dict = new Dictionary<string, object>();
            foreach (var kv in p.Payload)
            {
                object val = kv.Value.HasStringValue ? kv.Value.StringValue :
                    kv.Value.HasIntegerValue ? (object)kv.Value.IntegerValue :
                    kv.Value.HasDoubleValue ? (object)kv.Value.DoubleValue :
                    kv.Value.HasBoolValue ? (object)kv.Value.BoolValue :
                    kv.Value.ToString();
                dict[kv.Key] = val;
            }
            yield return new SearchHit(p.Id.ToString(), p.Score, dict);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
