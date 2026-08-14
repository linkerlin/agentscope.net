using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace AgentScope.Extensions.Store.Cos;

/// <summary>
/// 腾讯云 COS 对象存储适配器。对标 Java CosStore。
/// 通过 COS HTTP REST API 进行读写操作（不依赖 SDK 版本兼容性）。
/// 用于 Agent 状态持久化、会话存储、知识库备份。
/// </summary>
public sealed class CosStore : IDistributedStore
{
    private readonly HttpClient _http;
    private readonly string _bucketUrl;
    private readonly string _secretId;
    private readonly string _secretKey;
    private readonly string _region;

    public CosStore(HttpClient http, string bucketUrl, string secretId, string secretKey, string region = "ap-guangzhou")
    {
        _http = http;
        _bucketUrl = bucketUrl.TrimEnd('/');
        _secretId = secretId;
        _secretKey = secretKey;
        _region = region;
    }

    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        var url = $"{_bucketUrl}/{key}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        SignRequest(req, key);

        using var resp = await _http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var url = $"{_bucketUrl}/{key}";
        using var req = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(value, Encoding.UTF8, "application/json")
        };
        SignRequest(req, key);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        var url = $"{_bucketUrl}/{key}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        SignRequest(req, key);

        using var resp = await _http.SendAsync(req, ct);
        return resp.IsSuccessStatusCode;
    }

    public async IAsyncEnumerable<string> ListKeysAsync(string prefix,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 对标 Java CosBaseStore.listAllKeys：GET Bucket (List Objects) 分页列举，解析 XML
        var marker = "";
        do
        {
            var url = $"{_bucketUrl}/?prefix={Uri.EscapeDataString(prefix)}&max-keys=1000";
            if (!string.IsNullOrEmpty(marker))
                url += $"&marker={Uri.EscapeDataString(marker)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            SignRequest(req, prefix);
            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var xml = await resp.Content.ReadAsStringAsync(ct);
            foreach (var key in ParseListObjectKeys(xml))
                yield return key;

            marker = ParseNextMarker(xml);
        } while (!string.IsNullOrEmpty(marker));
    }

    private static IEnumerable<string> ParseListObjectKeys(string xml)
    {
        // 简单解析 ListBucketResult 中的 <Key> 元素
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(xml, "<Key>(.*?)</Key>"))
            yield return System.Net.WebUtility.HtmlDecode(m.Groups[1].Value);
    }

    private static string ParseNextMarker(string xml)
    {
        var match = System.Text.RegularExpressions.Regex.Match(
            xml, "<NextMarker>(.*?)</NextMarker>");
        if (!match.Success) return "";
        return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    /// <summary>COS 标准签名（HMAC-SHA1）。对标 Java COS Authorization 签名。</summary>
    private void SignRequest(HttpRequestMessage req, string key)
    {
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(5);
        var keyTime = $"{now.ToUnixTimeSeconds()};{expires.ToUnixTimeSeconds()}";
        var signKey = HmacSha1(_secretKey, keyTime);
        var httpString = $"{req.Method}\n/{key}\n\n\n";
        var sha1 = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(httpString));
        var stringToSign = $"sha1\n{keyTime}\n{BitConverter.ToString(sha1).Replace("-", "").ToLower()}\n";
        var signature = HmacSha1(signKey, stringToSign);

        req.Headers.Add("Authorization",
            $"q-sign-algorithm=sha1&q-ak={_secretId}&q-sign-time={keyTime}" +
            $"&q-key-time={keyTime}&q-header-list=&q-url-param-list=" +
            $"&q-signature={signature}");
        req.Headers.Add("Host", new Uri(_bucketUrl).Host);
    }

    private static string HmacSha1(string key, string data)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
