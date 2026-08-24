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

using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace AgentScope.Extensions.Store.Cos;

/// <summary>
/// Tencent Cloud COS (Cloud Object Storage) adapter.
/// Communicates with COS via its HTTP REST API to avoid SDK version compatibility issues.
/// Used for Agent state persistence, session storage, and knowledge base backups.
/// 腾讯云 COS 对象存储适配器。
/// 通过 COS HTTP REST API 进行读写操作（不依赖 SDK 版本兼容性）。
/// 用于 Agent 状态持久化、会话存储、知识库备份。
/// </summary>
/// <remarks>
/// This class implements <see cref="IDistributedStore"/> and provides the core
/// CRUD operations (Get, Set, Delete, ListKeys) against Tencent Cloud COS.
/// All HTTP requests are signed using the COS HMAC-SHA1 authorization scheme
/// (q-sign-algorithm=sha1). The implementation mirrors the Java CosStore class.
/// 该类实现了 <see cref="IDistributedStore"/>，提供了针对腾讯云 COS 的
/// 核心 CRUD 操作（Get、Set、Delete、ListKeys）。
/// 所有 HTTP 请求均使用 COS HMAC-SHA1 授权方案签名（q-sign-algorithm=sha1）。
/// 实现对标 Java CosStore 类。
/// </remarks>
public sealed class CosStore : IDistributedStore
{
    /// <summary>
    /// Shared HTTP client used for all COS REST API calls.
    /// 用于所有 COS REST API 调用的共享 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// Base bucket URL (e.g. "https://bucket-name.cos.ap-guangzhou.myqcloud.com").
    /// Trailing slash is trimmed during construction.
    /// 存储桶基础 URL（例如 "https://bucket-name.cos.ap-guangzhou.myqcloud.com"）。
    /// 构造时会去除尾部斜杠。
    /// </summary>
    private readonly string _bucketUrl;

    /// <summary>
    /// Tencent Cloud API secret ID for authentication.
    /// 腾讯云 API 密钥 ID，用于身份认证。
    /// </summary>
    private readonly string _secretId;

    /// <summary>
    /// Tencent Cloud API secret key for signing requests.
    /// 腾讯云 API 密钥，用于请求签名。
    /// </summary>
    private readonly string _secretKey;

    /// <summary>
    /// COS bucket region, e.g. "ap-guangzhou".
    /// COS 存储桶地域，例如 "ap-guangzhou"。
    /// </summary>
    private readonly string _region;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosStore"/> class.
    /// Creates a COS store adapter with the specified HTTP client and authentication credentials.
    /// 使用指定的 HTTP 客户端和认证凭据创建 COS 存储适配器实例。
    /// </summary>
    /// <param name="http">
    /// The <see cref="HttpClient"/> used for all COS REST API requests.
    /// Caller is responsible for its lifecycle (e.g. via IHttpClientFactory).
    /// 用于所有 COS REST API 请求的 <see cref="HttpClient"/>。
    /// 调用方负责其生命周期（例如通过 IHttpClientFactory）。
    /// </param>
    /// <param name="bucketUrl">
    /// The COS bucket endpoint URL (e.g. "https://bucket-name.cos.ap-guangzhou.myqcloud.com").
    /// COS 存储桶端点 URL。
    /// </param>
    /// <param name="secretId">
    /// Tencent Cloud API secret ID.
    /// 腾讯云 API 密钥 ID。
    /// </param>
    /// <param name="secretKey">
    /// Tencent Cloud API secret key.
    /// 腾讯云 API 密钥。
    /// </param>
    /// <param name="region">
    /// COS bucket region name. Defaults to "ap-guangzhou".
    /// COS 存储桶地域名称。默认值为 "ap-guangzhou"。
    /// </param>
    public CosStore(HttpClient http, string bucketUrl, string secretId, string secretKey, string region = "ap-guangzhou")
    {
        _http = http;
        _bucketUrl = bucketUrl.TrimEnd('/');
        _secretId = secretId;
        _secretKey = secretKey;
        _region = region;
    }

    /// <summary>
    /// Retrieves the value associated with the specified key from COS.
    /// Returns null if the key does not exist or an error occurs.
    /// 从 COS 获取指定键关联的值。如果键不存在或发生错误则返回 null。
    /// </summary>
    /// <param name="key">
    /// The object key within the COS bucket.
    /// COS 存储桶中的对象键。
    /// </param>
    /// <param name="ct">
    /// Cancellation token to cancel the operation.
    /// 用于取消操作的取消令牌。
    /// </param>
    /// <returns>
    /// The object content as a string, or null if not found.
    /// 对象内容的字符串形式，如果未找到则返回 null。
    /// </returns>
    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        // 构建对象完整 URL 并发送 GET 请求
        var url = $"{_bucketUrl}/{key}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        SignRequest(req, key);

        using var resp = await _http.SendAsync(req, ct);
        // 非成功状态码（如 404）视为键不存在，返回 null
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Stores a value under the specified key in COS.
    /// If the key already exists, it will be overwritten.
    /// 在 COS 中将值存储到指定键下。如果键已存在则覆盖。
    /// </summary>
    /// <param name="key">
    /// The object key within the COS bucket.
    /// COS 存储桶中的对象键。
    /// </param>
    /// <param name="value">
    /// The JSON string content to store.
    /// 要存储的 JSON 字符串内容。
    /// </param>
    /// <param name="ttl">
    /// Optional time-to-live. Note: COS does not natively support TTL on individual objects;
    /// this parameter is accepted for interface compatibility but may require lifecycle rules
    /// configured on the bucket to take effect.
    /// 可选的生存时间。注意：COS 本身不支持单个对象的 TTL；
    /// 此参数为了接口兼容而保留，实际生效需要依赖存储桶上配置的生命周期规则。
    /// </param>
    /// <param name="ct">
    /// Cancellation token to cancel the operation.
    /// 用于取消操作的取消令牌。
    /// </param>
    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        // PUT 请求写入对象内容，Content-Type 为 application/json
        var url = $"{_bucketUrl}/{key}";
        using var req = new HttpRequestMessage(HttpMethod.Put, url)
        {
            Content = new StringContent(value, Encoding.UTF8, "application/json")
        };
        SignRequest(req, key);

        using var resp = await _http.SendAsync(req, ct);
        // 非成功状态码会抛出异常
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Deletes the object identified by the specified key from COS.
    /// Returns true if the deletion was successful (including when the key did not exist).
    /// 从 COS 删除指定键标识的对象。
    /// 如果删除成功（包括键不存在的情况）则返回 true。
    /// </summary>
    /// <param name="key">
    /// The object key within the COS bucket to delete.
    /// 要删除的 COS 存储桶中的对象键。
    /// </param>
    /// <param name="ct">
    /// Cancellation token to cancel the operation.
    /// 用于取消操作的取消令牌。
    /// </param>
    /// <returns>
    /// True if the server returned a success status code (2xx); false otherwise.
    /// 如果服务器返回成功状态码（2xx）则为 true，否则为 false。
    /// </returns>
    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        // DELETE 请求移除指定对象
        var url = $"{_bucketUrl}/{key}";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        SignRequest(req, key);

        using var resp = await _http.SendAsync(req, ct);
        // COS 删除不存在的对象也返回 204 No Content，所以直接检查状态码即可
        return resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// Lists all object keys in the COS bucket that start with the specified prefix.
    /// Uses paginated GET Bucket (List Objects) requests (max 1000 keys per page).
    /// Corresponds to Java CosBaseStore.listAllKeys.
    /// 列举 COS 存储桶中所有以指定前缀开头的对象键。
    /// 使用分页的 GET Bucket（List Objects）请求（每页最多 1000 个键）。
    /// 对标 Java CosBaseStore.listAllKeys。
    /// </summary>
    /// <param name="prefix">
    /// Key prefix to filter objects (empty string matches all objects).
    /// 用于过滤对象的键前缀（空字符串匹配所有对象）。
    /// </param>
    /// <param name="ct">
    /// Cancellation token to cancel the enumeration.
    /// 用于取消枚举操作的取消令牌。
    /// </param>
    /// <returns>
    /// An async-enumerable sequence of matching object keys.
    /// 匹配的对象键的异步可枚举序列。
    /// </returns>
    public async IAsyncEnumerable<string> ListKeysAsync(string prefix,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 对标 Java CosBaseStore.listAllKeys：GET Bucket (List Objects) 分页列举，解析 XML
        // marker 为空说明是第一页，非空则携带上一页返回的 NextMarker 继续翻页
        var marker = "";
        do
        {
            // 构造 List Objects 请求：指定前缀、每页最多 1000 条、可选的翻页标记
            var url = $"{_bucketUrl}/?prefix={Uri.EscapeDataString(prefix)}&max-keys=1000";
            if (!string.IsNullOrEmpty(marker))
                url += $"&marker={Uri.EscapeDataString(marker)}";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            SignRequest(req, prefix);
            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            // 解析返回的 XML，提取当前页的所有 <Key> 元素
            var xml = await resp.Content.ReadAsStringAsync(ct);
            foreach (var key in ParseListObjectKeys(xml))
                yield return key;

            // 获取下一页标记，如果为空说明已列举完毕
            marker = ParseNextMarker(xml);
        } while (!string.IsNullOrEmpty(marker));
    }

    /// <summary>
    /// Parses COS List Bucket XML response and extracts all &lt;Key&gt; element values.
    /// Uses simple regex-based parsing (no XML DOM overhead).
    /// 解析 COS List Bucket XML 响应，提取所有 &lt;Key&gt; 元素的值。
    /// 使用基于正则表达式的简单解析（避免 XML DOM 开销）。
    /// </summary>
    /// <param name="xml">
    /// The raw XML response from the COS GET Bucket request.
    /// COS GET Bucket 请求返回的原始 XML 响应。
    /// </param>
    /// <returns>
    /// An enumeration of decoded object key strings.
    /// 解码后的对象键字符串枚举。
    /// </returns>
    private static IEnumerable<string> ParseListObjectKeys(string xml)
    {
        // 简单解析 ListBucketResult 中的 <Key> 元素
        // 使用正则匹配 <Key>...</Key>，并对 HTML 编码的字符进行解码
        foreach (System.Text.RegularExpressions.Match m in
                 System.Text.RegularExpressions.Regex.Matches(xml, "<Key>(.*?)</Key>"))
            yield return System.Net.WebUtility.HtmlDecode(m.Groups[1].Value);
    }

    /// <summary>
    /// Extracts the &lt;NextMarker&gt; value from a COS List Bucket XML response.
    /// Returns an empty string if there are no more results (i.e., listing is complete).
    /// 从 COS List Bucket XML 响应中提取 &lt;NextMarker&gt; 值。
    /// 如果没有更多结果（即列举完成）则返回空字符串。
    /// </summary>
    /// <param name="xml">
    /// The raw XML response from the COS GET Bucket request.
    /// COS GET Bucket 请求返回的原始 XML 响应。
    /// </param>
    /// <returns>
    /// The next marker token, or empty string if listing is complete.
    /// 下一页的标记令牌，如果列举完成则为空字符串。
    /// </returns>
    private static string ParseNextMarker(string xml)
    {
        // 提取 <NextMarker> 元素值，为空说明没有下一页了
        var match = System.Text.RegularExpressions.Regex.Match(
            xml, "<NextMarker>(.*?)</NextMarker>");
        if (!match.Success) return "";
        return System.Net.WebUtility.HtmlDecode(match.Groups[1].Value);
    }

    /// <summary>
    /// Signs an HTTP request with COS standard HMAC-SHA1 authorization header.
    /// Corresponds to the Java COS Authorization signature algorithm.
    /// 使用 COS 标准 HMAC-SHA1 授权头对 HTTP 请求进行签名。
    /// 对标 Java COS Authorization 签名算法。
    /// </summary>
    /// <remarks>
    /// The signature scheme is q-sign-algorithm=sha1:
    /// 1. Compute keyTime (current time to current time + 5 minutes, in Unix seconds).
    /// 2. Derive signKey = HMAC-SHA1(secretKey, keyTime).
    /// 3. Compute SHA1 hash of the HTTP method + path.
    /// 4. Build stringToSign = "sha1\n{keyTime}\n{sha1hex}\n".
    /// 5. Derive signature = HMAC-SHA1(signKey, stringToSign).
    /// 6. Assemble the Authorization header with all parameters.
    /// 签名方案为 q-sign-algorithm=sha1：
    /// 1. 计算 keyTime（当前时间到当前时间 + 5 分钟，Unix 秒数）。
    /// 2. 派生 signKey = HMAC-SHA1(secretKey, keyTime)。
    /// 3. 计算 HTTP 方法 + 路径的 SHA1 哈希。
    /// 4. 构建 stringToSign = "sha1\n{keyTime}\n{sha1hex}\n"。
    /// 5. 派生 signature = HMAC-SHA1(signKey, stringToSign)。
    /// 6. 组装包含所有参数的 Authorization 头。
    /// </remarks>
    /// <param name="req">
    /// The HTTP request message to sign. The Authorization header will be added.
    /// 需要签名的 HTTP 请求消息。将添加 Authorization 头。
    /// </param>
    /// <param name="key">
    /// The object key (path) being accessed, used in the signature computation.
    /// 正在访问的对象键（路径），用于签名计算。
    /// </param>
    private void SignRequest(HttpRequestMessage req, string key)
    {
        // 步骤1：计算签名有效时间窗口（当前时间 + 5 分钟）
        var now = DateTimeOffset.UtcNow;
        var expires = now.AddMinutes(5);
        var keyTime = $"{now.ToUnixTimeSeconds()};{expires.ToUnixTimeSeconds()}";

        // 步骤2：使用 SecretKey 对 keyTime 进行 HMAC-SHA1 得到 signKey
        var signKey = HmacSha1(_secretKey, keyTime);

        // 步骤3：构建 HTTP 请求的字符串表示并计算其 SHA1 哈希
        var httpString = $"{req.Method}\n/{key}\n\n\n";
        var sha1 = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(httpString));
        var stringToSign = $"sha1\n{keyTime}\n{BitConverter.ToString(sha1).Replace("-", "").ToLower()}\n";

        // 步骤4：使用 signKey 对 stringToSign 进行 HMAC-SHA1 得到最终签名
        var signature = HmacSha1(signKey, stringToSign);

        // 步骤5：组装 Authorization 请求头
        // 包含签名算法、AK、时间范围、签名值等参数
        req.Headers.Add("Authorization",
            $"q-sign-algorithm=sha1&q-ak={_secretId}&q-sign-time={keyTime}" +
            $"&q-key-time={keyTime}&q-header-list=&q-url-param-list=" +
            $"&q-signature={signature}");

        // 添加 Host 头，COS 要求必须携带
        req.Headers.Add("Host", new Uri(_bucketUrl).Host);
    }

    /// <summary>
    /// Computes the HMAC-SHA1 hash of the input data using the specified key.
    /// Returns the hash as a lowercase hex string.
    /// 使用指定密钥计算输入数据的 HMAC-SHA1 哈希。
    /// 返回小写十六进制字符串形式的哈希值。
    /// </summary>
    /// <param name="key">
    /// The HMAC secret key.
    /// HMAC 密钥。
    /// </param>
    /// <param name="data">
    /// The data to be hashed.
    /// 要进行哈希计算的数据。
    /// </param>
    /// <returns>
    /// The HMAC-SHA1 hash as a lowercase hex string (e.g. "a1b2c3d4...").
    /// 小写十六进制字符串表示的 HMAC-SHA1 哈希值（如 "a1b2c3d4..."）。
    /// </returns>
    private static string HmacSha1(string key, string data)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        // 将字节数组转换为不带分隔符的小写十六进制字符串
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}
