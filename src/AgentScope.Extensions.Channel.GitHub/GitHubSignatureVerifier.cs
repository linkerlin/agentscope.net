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

using System.Security.Cryptography;
using System.Text;

namespace AgentScope.Extensions.Channel.GitHub;

/// <summary>
/// 校验 GitHub webhook 的 <c>X-Hub-Signature-256</c> 头：以 <c>sha256=</c> 前缀开头，
/// 后接以 webhook secret 为密钥、对原始请求体计算的 HMAC-SHA256 的十六进制值。
/// 对应 Java: io.agentscope.extensions.channel.github.GitHubSignatureVerifier
/// </summary>
public sealed class GitHubSignatureVerifier
{
    private const string Prefix = "sha256=";

    private readonly byte[] _secret;

    public GitHubSignatureVerifier(string webhookSecret)
    {
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            throw new ArgumentException("webhookSecret is required", nameof(webhookSecret));
        }
        _secret = Encoding.UTF8.GetBytes(webhookSecret);
    }

    /// <summary>当 <c>header</c> 匹配 <c>sha256= + hex(HMAC-SHA256(secret, rawBody))</c> 时返回 true（常量时间比较）。</summary>
    public bool Verify(string? header, byte[] rawBody)
    {
        if (header is null || !header.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }
        var digest = HMACSHA256.HashData(_secret, rawBody);
        var expectedHex = Convert.ToHexString(digest).ToLowerInvariant();
        var got = header.Substring(Prefix.Length);
        return ConstantTimeEquals(expectedHex, got);
    }

    private static bool ConstantTimeEquals(string? a, string? b)
    {
        if (a is null || b is null)
        {
            return false;
        }
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();
        if (a.Length != b.Length)
        {
            return false;
        }
        int r = 0;
        for (int i = 0; i < a.Length; i++)
        {
            r |= a[i] ^ b[i];
        }
        return r == 0;
    }
}
