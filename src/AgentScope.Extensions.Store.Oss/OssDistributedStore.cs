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
using System.Xml.Linq;

namespace AgentScope.Extensions.Store.Oss;

/// <summary>
/// A distributed key-value store that uses Alibaba Cloud OSS as the backing
/// storage. Keys are mapped to OSS object paths; values are stored as UTF-8
/// text content. Supports Get, Set, Delete, and prefix-based List operations.
/// 一个基于阿里云 OSS 的分布式键值存储实现。
/// 键映射为 OSS 对象路径，值以 UTF-8 文本内容存储。
/// 支持 Get、Set、Delete 以及基于前缀的 List 操作。
/// </summary>
/// <remarks>
/// This store uses the OSS REST API over HTTP(S). Object URLs follow the
/// pattern <c>{endpoint}/{bucket}/{key}</c>. Listing uses S3-compatible XML
/// response parsing.
/// 该存储通过 HTTP(S) 调用 OSS REST API。对象 URL 遵循
/// <c>{endpoint}/{bucket}/{key}</c> 格式。列表操作通过解析
/// 兼容 S3 的 XML 响应实现。
/// </remarks>
public sealed class OssDistributedStore : IDistributedStore
{
    /// <summary>
    /// HTTP client used to send requests to the OSS service.
    /// 用于向 OSS 服务发送请求的 HTTP 客户端。
    /// </summary>
    private readonly HttpClient _http;

    /// <summary>
    /// OSS endpoint URL (e.g. "https://oss-cn-hangzhou.aliyuncs.com").
    /// OSS 端点 URL（例如 "https://oss-cn-hangzhou.aliyuncs.com"）。
    /// </summary>
    private readonly string _endpoint;

    /// <summary>
    /// OSS bucket name.
    /// OSS 存储桶名称。
    /// </summary>
    private readonly string _bucket;

    /// <summary>
    /// Alibaba Cloud AccessKey ID for authentication.
    /// 阿里云访问密钥 ID，用于身份认证。
    /// </summary>
    private readonly string _accessKeyId;

    /// <summary>
    /// Alibaba Cloud AccessKey Secret for authentication.
    /// 阿里云访问密钥 Secret，用于身份认证。
    /// </summary>
    private readonly string _accessKeySecret;

    /// <summary>
    /// Initializes a new instance of the <see cref="OssDistributedStore"/> class.
    /// 初始化 <see cref="OssDistributedStore"/> 类的新实例。
    /// </summary>
    /// <param name="http">The <see cref="HttpClient"/> for OSS REST API calls /
    /// 用于 OSS REST API 调用的 <see cref="HttpClient"/></param>
    /// <param name="endpoint">OSS endpoint (e.g. "https://oss-cn-hangzhou.aliyuncs.com") /
    /// OSS 端点地址</param>
    /// <param name="bucket">OSS bucket name / OSS 存储桶名称</param>
    /// <param name="accessKeyId">Alibaba Cloud AccessKey ID / 阿里云 AccessKey ID</param>
    /// <param name="accessKeySecret">Alibaba Cloud AccessKey Secret /
    /// 阿里云 AccessKey Secret</param>
    public OssDistributedStore(HttpClient http, string endpoint, string bucket, string accessKeyId, string accessKeySecret)
    {
        _http = http;
        _endpoint = endpoint.TrimEnd('/'); // 移除末尾斜杠以保证 URL 拼接正确
        _bucket = bucket;
        _accessKeyId = accessKeyId;
        _accessKeySecret = accessKeySecret;
    }

    /// <summary>
    /// Builds the full OSS object URL from a key.
    /// 根据键构建完整的 OSS 对象 URL。
    /// </summary>
    /// <param name="key">The object key / 对象键</param>
    /// <returns>The full object URL / 完整的对象 URL</returns>
    private string ObjectUrl(string key) => $"{_endpoint}/{_bucket}/{key}";

    /// <summary>
    /// Retrieves the value associated with the specified key from OSS.
    /// 从 OSS 中获取指定键关联的值。
    /// </summary>
    /// <param name="key">The object key to retrieve / 要获取的对象键</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>
    /// The value as a string, or <c>null</c> if the key does not exist.
    /// 字符串形式的值，如果键不存在则返回 <c>null</c>。
    /// </returns>
    public async ValueTask<string?> GetAsync(string key, CancellationToken ct = default)
    {
        // 发送 GET 请求获取 OSS 对象
        using var req = new HttpRequestMessage(HttpMethod.Get, ObjectUrl(key));
        using var resp = await _http.SendAsync(req, ct);
        // 如果对象不存在（404）或其他非成功状态码，返回 null
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadAsStringAsync(ct);
    }

    /// <summary>
    /// Stores a value under the specified key in OSS.
    /// 在 OSS 中存储指定键对应的值。
    /// </summary>
    /// <param name="key">The object key to store / 要存储的对象键</param>
    /// <param name="value">The value to store (UTF-8 text) / 要存储的值（UTF-8 文本）</param>
    /// <param name="ttl">
    /// Optional time-to-live (currently not implemented for OSS;
    /// reserved for future use).
    /// 可选的生存时间（当前 OSS 实现未使用，保留给未来扩展）。
    /// </param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async ValueTask SetAsync(string key, string value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        // 发送 PUT 请求将对象内容写入 OSS
        using var req = new HttpRequestMessage(HttpMethod.Put, ObjectUrl(key))
        { Content = new StringContent(value) };
        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Deletes the object identified by the specified key from OSS.
    /// 从 OSS 中删除指定键标识的对象。
    /// </summary>
    /// <param name="key">The object key to delete / 要删除的对象键</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>
    /// <c>true</c> if the deletion succeeded or the key did not exist;
    /// <c>false</c> if the request failed.
    /// 如果删除成功或键不存在则返回 <c>true</c>；
    /// 如果请求失败则返回 <c>false</c>。
    /// </returns>
    public async ValueTask<bool> DeleteAsync(string key, CancellationToken ct = default)
    {
        // 发送 DELETE 请求删除 OSS 对象
        using var req = new HttpRequestMessage(HttpMethod.Delete, ObjectUrl(key));
        using var resp = await _http.SendAsync(req, ct);
        // 只要返回成功状态码（包括 204 No Content）即视为删除成功
        return resp.IsSuccessStatusCode;
    }

    /// <summary>
    /// Lists all object keys under the given prefix. Uses OSS's S3-compatible
    /// listing API and parses the returned XML. Each call retrieves up to 1000
    /// keys (the OSS max-keys limit).
    /// 列出指定前缀下的所有对象键。使用 OSS 兼容 S3 的列表 API，
    /// 并解析返回的 XML。每次调用最多获取 1000 个键（OSS 的 max-keys 上限）。
    /// </summary>
    /// <param name="prefix">The key prefix to filter by / 用于筛选的键前缀</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>
    /// An async-enumerable sequence of matching object keys.
    /// 匹配的对象键的异步可枚举序列。
    /// </returns>
    public async IAsyncEnumerable<string> ListKeysAsync(string prefix, [EnumeratorCancellation] CancellationToken ct = default)
    {
        // 构造带前缀和 max-keys 参数的 GET 请求 URL
        var url = $"{_endpoint}/{_bucket}?prefix={prefix}&max-keys=1000";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await _http.SendAsync(req, ct);
        // 如果请求失败（如前缀不存在），直接结束枚举
        if (!resp.IsSuccessStatusCode) yield break;

        // 解析 OSS 返回的 S3-compatible XML 响应
        var xml = await resp.Content.ReadAsStringAsync(ct);
        var doc = XDocument.Parse(xml);
        // 提取所有 <Key> 元素的值并逐个产出
        foreach (var el in doc.Descendants("{http://s3.amazonaws.com/doc/2006-03-01/}Key"))
            yield return el.Value;
    }
}
