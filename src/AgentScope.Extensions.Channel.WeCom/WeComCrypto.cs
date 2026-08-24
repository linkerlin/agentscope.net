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

namespace AgentScope.Extensions.Channel.WeCom;

/// <summary>
/// 实现企业微信的 URL 握手签名 + AES-CBC + PKCS#7 加解密方案。
/// 对应 Java: io.agentscope.extensions.channel.wecom.WeComCrypto
/// </summary>
/// <remarks>
/// 输入：
/// <list type="bullet">
/// <item><c>token</c> — 开发者配置的回调 token。</item>
/// <item><c>encodingAesKey</c> — 43 字符 base64（无填充）。追加 <c>"="</c> 后解码得到 32 字节 AES key；前 16 字节同时作为 CBC IV。</item>
/// <item><c>receiveId</c> — 企业 id（自建应用）或 suite id（ISV 应用）。解密时校验尾部 receive-id。</item>
/// </list>
/// </remarks>
public sealed class WeComCrypto
{
    private readonly string _token;
    private readonly byte[] _aesKey; // 32 bytes
    private readonly byte[] _iv;    // first 16 bytes of aesKey
    private readonly string _receiveId;

    public WeComCrypto(string token, string encodingAesKey, string receiveId)
    {
        if (token is null)
        {
            throw new ArgumentNullException(nameof(token), "token is required");
        }
        if (encodingAesKey is null || encodingAesKey.Length != 43)
        {
            throw new ArgumentException(
                "encodingAesKey must be 43 characters (got " + (encodingAesKey?.Length ?? 0) + ")", nameof(encodingAesKey));
        }
        if (receiveId is null)
        {
            throw new ArgumentNullException(nameof(receiveId), "receiveId is required");
        }

        _token = token;
        try
        {
            _aesKey = Convert.FromBase64String(encodingAesKey + "=");
        }
        catch (FormatException e)
        {
            throw new ArgumentException("encodingAesKey is not valid base64: " + e.Message, nameof(encodingAesKey));
        }
        if (_aesKey.Length != 32)
        {
            throw new ArgumentException("Decoded AES key must be 32 bytes (got " + _aesKey.Length + ")", nameof(encodingAesKey));
        }
        _iv = new byte[16];
        Array.Copy(_aesKey, 0, _iv, 0, 16);
        _receiveId = receiveId;
    }

    /// <summary>校验 SHA-1 签名：将 [token, timestamp, nonce, encrypt] 排序后拼接再哈希。</summary>
    public bool VerifySignature(string? signature, string? timestamp, string? nonce, string? encrypt)
    {
        if (signature is null || timestamp is null || nonce is null || encrypt is null)
        {
            return false;
        }
        var parts = new[] { _token, timestamp, nonce, encrypt };
        Array.Sort(parts, StringComparer.Ordinal);
        var joined = string.Concat(parts);
        var digest = SHA1.HashData(Encoding.UTF8.GetBytes(joined));
        return ConstantTimeEquals(Convert.ToHexString(digest).ToLowerInvariant(), signature);
    }

    /// <summary>
    /// 解密 <c>Encrypt</c> 体并返回内层 XML 明文（UTF-8 字符串）。
    /// 明文布局：<c>| 16 字节 random | 4 字节 msg_len（大端）| msg（XML）| receive_id |</c>。
    /// </summary>
    public string Decrypt(string encryptBase64)
    {
        byte[] cipherBytes;
        try
        {
            cipherBytes = Convert.FromBase64String(encryptBase64);
        }
        catch (FormatException e)
        {
            throw new InvalidOperationException("WeCom decrypt failed: invalid base64: " + e.Message, e);
        }

        byte[] unpad;
        try
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.IV = _iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            using var decryptor = aes.CreateDecryptor();
            var plain = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
            unpad = Pkcs7Unpad(plain);
        }
        catch (CryptographicException e)
        {
            throw new InvalidOperationException("WeCom decrypt failed: " + e.Message, e);
        }

        if (unpad.Length < 20)
        {
            throw new InvalidOperationException("Decrypted payload too short");
        }
        int msgLen =
            (unpad[16] & 0xff) << 24
            | (unpad[17] & 0xff) << 16
            | (unpad[18] & 0xff) << 8
            | (unpad[19] & 0xff);
        if (msgLen < 0 || 20 + msgLen > unpad.Length)
        {
            throw new InvalidOperationException("Invalid msg_len in decrypted payload: " + msgLen);
        }

        var xml = Encoding.UTF8.GetString(unpad, 20, msgLen);
        var trailingReceiveId = Encoding.UTF8.GetString(unpad, 20 + msgLen, unpad.Length - 20 - msgLen);
        if (!string.Equals(_receiveId, trailingReceiveId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "receive_id mismatch (expected '" + _receiveId + "', got '" + trailingReceiveId + "')");
        }
        return xml;
    }

    private static byte[] Pkcs7Unpad(byte[] input)
    {
        if (input.Length == 0)
        {
            return input;
        }
        int pad = input[^1] & 0xff;
        if (pad < 1 || pad > 32 || pad > input.Length)
        {
            return input;
        }
        var result = new byte[input.Length - pad];
        Array.Copy(input, result, result.Length);
        return result;
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
        int result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }
        return result == 0;
    }
}
