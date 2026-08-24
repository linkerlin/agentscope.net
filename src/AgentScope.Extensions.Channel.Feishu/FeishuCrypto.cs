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

namespace AgentScope.Extensions.Channel.Feishu;

/// <summary>
/// 实现飞书回调加密 + 签名校验。
/// 对应 Java: io.agentscope.extensions.channel.feishu.FeishuCrypto
/// </summary>
/// <remarks>
/// 飞书在开发者后台配置 Encrypt Key 后会对回调体加密，线格式为 JSON <c>{"encrypt":"&lt;base64&gt;"}</c>。
/// AES 密钥派生为 <c>SHA-256(encryptKey)</c>（32 字节）。密文布局：
/// <c>| 16 字节 IV | AES-CBC + PKCS#7 填充 body |</c>。
/// 签名头 <c>X-Lark-Signature</c> = <c>SHA-256(timestamp + nonce + encryptKey + rawBody)</c>。
/// </remarks>
public sealed class FeishuCrypto
{
    /// <summary>Raw encrypt key string for signature calculation / 用于签名计算的原始加密密钥字符串</summary>
    private readonly string _encryptKey;

    /// <summary>AES key derived from SHA-256 of encryptKey (32 bytes) / 从 encryptKey 的 SHA-256 派生的 AES 密钥（32 字节）</summary>
    private readonly byte[] _aesKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeishuCrypto"/> class.
    /// 初始化 <see cref="FeishuCrypto"/> 类的新实例。
    /// </summary>
    /// <param name="encryptKey">The encrypt key configured in Feishu developer console / 飞书开发者后台配置的加密密钥</param>
    /// <exception cref="ArgumentException">Thrown when encryptKey is null or whitespace / 当 encryptKey 为 null 或空白时抛出</exception>
    public FeishuCrypto(string encryptKey)
    {
        if (string.IsNullOrWhiteSpace(encryptKey))
        {
            throw new ArgumentException("encryptKey is required", nameof(encryptKey));
        }
        _encryptKey = encryptKey;
        _aesKey = SHA256.HashData(Encoding.UTF8.GetBytes(encryptKey));
    }

    /// <summary>
    /// Verifies the SHA-256 signature: <c>hex(SHA-256(timestamp + nonce + encryptKey + body))</c>.
    /// Returns false if any parameter is null or the digest does not match.
    /// 校验 SHA-256 签名：<c>hex(SHA-256(timestamp + nonce + encryptKey + body))</c>。
    /// 任一参数为 null 或摘要不匹配时返回 false。
    /// </summary>
    /// <param name="signature">The signature from X-Lark-Signature header / X-Lark-Signature 请求头中的签名</param>
    /// <param name="timestamp">The timestamp from X-Lark-Request-Timestamp header / X-Lark-Request-Timestamp 请求头中的时间戳</param>
    /// <param name="nonce">The nonce from X-Lark-Request-Nonce header / X-Lark-Request-Nonce 请求头中的随机数</param>
    /// <param name="body">The raw request body / 原始请求体</param>
    /// <returns>True if the signature is valid, false otherwise / 签名有效时返回 true，否则返回 false</returns>
    public bool VerifySignature(string? signature, string? timestamp, string? nonce, string? body)
    {
        if (signature is null || timestamp is null || nonce is null || body is null)
        {
            return false;
        }

        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(Encoding.UTF8.GetBytes(timestamp));
        sha.AppendData(Encoding.UTF8.GetBytes(nonce));
        sha.AppendData(Encoding.UTF8.GetBytes(_encryptKey));
        sha.AppendData(Encoding.UTF8.GetBytes(body));
        var digest = sha.GetHashAndReset();
        return ConstantTimeEquals(ToHexLower(digest), signature);
    }

    /// <summary>
    /// Decrypts the Feishu callback <c>encrypt</c> field, returning the UTF-8 JSON plaintext.
    /// Layout: <c>| 16-byte IV | AES-256-CBC + PKCS#7 padded plaintext |</c>.
    /// 解密飞书回调的 <c>encrypt</c> 字段，返回 JSON 明文的 UTF-8 字符串。
    /// 布局：<c>| 16 字节 IV | AES-256-CBC + PKCS#7 填充明文 |</c>。
    /// </summary>
    /// <param name="encryptBase64">The base64-encoded encrypted payload / Base64 编码的加密负载</param>
    /// <returns>Decrypted UTF-8 JSON string / 解密后的 UTF-8 JSON 字符串</returns>
    /// <exception cref="InvalidOperationException">Thrown when decryption fails / 解密失败时抛出</exception>
    public string Decrypt(string encryptBase64)
    {
        byte[] cipherBytes;
        try
        {
            cipherBytes = Convert.FromBase64String(encryptBase64);
        }
        catch (FormatException e)
        {
            throw new InvalidOperationException("Feishu decrypt failed: invalid base64: " + e.Message, e);
        }
        if (cipherBytes.Length < 32)
        {
            throw new InvalidOperationException("Encrypted payload too short");
        }

        var iv = new byte[16];
        Array.Copy(cipherBytes, 0, iv, 0, 16);
        var body = new byte[cipherBytes.Length - 16];
        Array.Copy(cipherBytes, 16, body, 0, body.Length);

        try
        {
            using var aes = Aes.Create();
            aes.Key = _aesKey;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.None;
            using var decryptor = aes.CreateDecryptor();
            var plain = decryptor.TransformFinalBlock(body, 0, body.Length);
            var unpadded = Pkcs7Unpad(plain);
            return Encoding.UTF8.GetString(unpadded);
        }
        catch (CryptographicException e)
        {
            throw new InvalidOperationException("Feishu decrypt failed: " + e.Message, e);
        }
    }

    /// <summary>
    /// Removes PKCS#7 padding from decrypted byte array.
    /// 从解密后的字节数组中移除 PKCS#7 填充。
    /// </summary>
    /// <param name="input">Decrypted data with PKCS#7 padding / 含 PKCS#7 填充的解密数据</param>
    /// <returns>Unpadded byte array / 移除填充后的字节数组</returns>
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

    /// <summary>
    /// Converts byte array to lowercase hexadecimal string.
    /// 将字节数组转换为小写十六进制字符串。
    /// </summary>
    /// <param name="bytes">Input byte array / 输入字节数组</param>
    /// <returns>Lowercase hex string / 小写十六进制字符串</returns>
    private static string ToHexLower(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Compares two strings in constant time to prevent timing attacks.
    /// 以常量时间比较两个字符串，防止时序攻击。
    /// </summary>
    /// <param name="a">First string / 第一个字符串</param>
    /// <param name="b">Second string / 第二个字符串</param>
    /// <returns>True if both strings are equal, false otherwise / 两字符串相等时返回 true，否则 false</returns>
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
