using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace AgentScope.Extensions.Store.Oss;

public sealed class OssDistributedStore : IDistributedStore
{
    private readonly HttpClient _http;
    private readonly string _endpoint;
    private readonly string _bucket;
    private readonly string _accessKeyId;
    private readonly string _accessKeySecret;

    public OssDistributedStore(HttpClient http, string endpoint, string bucket, string accessKeyId, string accessKeySecret)
    {
        _http = http;
        _endpoint = endpoint.TrimEnd('/');
        _bucket = bucket;
        _accessKeyId = accessKeyId;
        _accessKeySecret = accessKeySecret;
    }

    private string ObjectUrl(string key) => $"{_endpoint}/{_bucket}/{key}";

    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, ObjectUrl(key));
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Put, ObjectUrl(key))
        { Content = new StringContent(value) };
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Delete, ObjectUrl(key));
        using var resp = await _http.SendAsync(req, ct);
        return resp.IsSuccessStatusCode;
    }

    public async IAsyncEnumerable<string> ListKeysAsync(string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var url = $"{_endpoint}/{_bucket}?prefix={prefix}&max-keys=1000";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) yield break;

        var xml = await resp.Content.ReadAsStringAsync(ct);
        var doc = XDocument.Parse(xml);
        foreach (var el in doc.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Key"))
            yield return el.Value;
    }
}

