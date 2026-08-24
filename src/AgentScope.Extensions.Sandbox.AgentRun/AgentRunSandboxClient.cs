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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace AgentScope.Extensions.Sandbox.AgentRun;

/// <summary>
/// Alibaba Cloud AgentRun sandbox client (thin factory).
/// Responsible for creating/resuming AgentRun sandboxes and serializing/deserializing sandbox state.
/// Counterpart of Java AgentRunSandboxClient.
/// <br/>
/// 阿里云 AgentRun 沙箱客户端（薄工厂）。
/// 负责创建/恢复 AgentRun 沙箱，以及序列化/反序列化沙箱状态。
/// 对标 Java AgentRunSandboxClient。
/// </summary>
public sealed class AgentRunSandboxClient : ISandboxClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new WorkspaceEntryJsonConverter() },
    };

    private readonly AgentRunSandboxClientOptions _options;
    private readonly HttpClient _http;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentRunSandboxClient"/> class.
    /// 初始化 AgentRunSandboxClient 实例。
    /// </summary>
    /// <param name="options">Client options (uses defaults if null) / 客户端选项（null 时使用默认值）</param>
    /// <param name="http">HttpClient (creates a new one if null) / HttpClient（null 时新建）</param>
    public AgentRunSandboxClient(AgentRunSandboxClientOptions? options = null, HttpClient? http = null)
    {
        _options = options ?? new AgentRunSandboxClientOptions();
        _http = http ?? new HttpClient();
    }

    /// <summary>
    /// Creates a new AgentRun sandbox instance.
    /// 创建一个新的 AgentRun 沙箱实例。
    /// </summary>
    /// <param name="spec">Workspace specification / 工作区规格</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>A new sandbox instance / 新的沙箱实例</returns>
    public Task<ISandbox> CreateAsync(WorkspaceSpec spec, CancellationToken ct = default)
    {
        var sandbox = new AgentRunSandbox(_http, _options.ResolvedDataPlaneBaseUrl);
        return Task.FromResult<ISandbox>(sandbox);
    }

    /// <summary>
    /// Resumes an existing AgentRun sandbox from saved state.
    /// 从保存的状态恢复一个已存在的 AgentRun 沙箱。
    /// </summary>
    /// <param name="state">Serialized sandbox state / 序列化的沙箱状态</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    /// <returns>The resumed sandbox instance / 恢复后的沙箱实例</returns>
    public Task<ISandbox> ResumeAsync(SandboxState state, CancellationToken ct = default)
    {
        var s = AgentRunSandboxState.FromSandboxState(state);
        var sandbox = new AgentRunSandbox(_http, _options.ResolvedDataPlaneBaseUrl);
        return Task.FromResult<ISandbox>(sandbox);
    }

    /// <summary>
    /// Deletes a sandbox by stopping and disposing it.
    /// 通过停止并释放沙箱来删除它。
    /// </summary>
    /// <param name="sandbox">The sandbox to delete / 要删除的沙箱</param>
    /// <param name="ct">Cancellation token / 取消令牌</param>
    public async Task DeleteAsync(ISandbox sandbox, CancellationToken ct = default)
    {
        if (sandbox == null) return;
        try { await sandbox.StopAsync(ct).ConfigureAwait(false); } catch { }
        try { await sandbox.DisposeAsync().ConfigureAwait(false); } catch { }
    }

    /// <summary>
    /// Serializes sandbox state to a JSON string.
    /// 将沙箱状态序列化为 JSON 字符串。
    /// </summary>
    /// <param name="state">Sandbox state to serialize / 要序列化的沙箱状态</param>
    /// <returns>JSON string representation / JSON 字符串表示</returns>
    public string SerializeState(SandboxState state) => JsonSerializer.Serialize(state, JsonOpts);

    /// <summary>
    /// Deserializes sandbox state from a JSON string.
    /// 从 JSON 字符串反序列化沙箱状态。
    /// </summary>
    /// <param name="json">JSON string of sandbox state / 沙箱状态的 JSON 字符串</param>
    /// <returns>Deserialized sandbox state / 反序列化后的沙箱状态</returns>
    /// <exception cref="InvalidOperationException">Thrown when deserialization fails / 反序列化失败时抛出</exception>
    public SandboxState DeserializeState(string json)
        => JsonSerializer.Deserialize<SandboxState>(json, JsonOpts)
           ?? throw new InvalidOperationException("Failed to deserialize AgentRun sandbox state.");

    /// <summary>
    /// Custom JSON converter for polymorphic serialization of <see cref="WorkspaceEntry"/> (FileEntry/DirEntry).
    /// 处理 <see cref="WorkspaceEntry"/> 多态序列化（FileEntry/DirEntry）的自定义 JSON 转换器。
    /// </summary>
    private sealed class WorkspaceEntryJsonConverter : JsonConverter<WorkspaceEntry>
    {
        /// <summary>
        /// Reads and deserializes a WorkspaceEntry from JSON, distinguishing DirEntry from FileEntry by the "Kind" property.
        /// 从 JSON 读取并反序列化 WorkspaceEntry，通过 "Kind" 属性区分 DirEntry 和 FileEntry。
        /// </summary>
        public override WorkspaceEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;
            var path = root.TryGetProperty("Path", out var p) ? p.GetString() ?? "" : "";
            var ephemeral = root.TryGetProperty("Ephemeral", out var e) && e.ValueKind == JsonValueKind.True;
            var kind = root.TryGetProperty("Kind", out var k) ? k.GetString() : null;
            if (kind == "dir")
                return new DirEntry(path, ephemeral);
            var content = root.TryGetProperty("Content", out var c) ? c.GetString() ?? "" : "";
            return new FileEntry(path, content, ephemeral);
        }

        /// <summary>
        /// Writes a WorkspaceEntry to JSON, including Kind discriminator and Content for FileEntry.
        /// 将 WorkspaceEntry 写入 JSON，包含 Kind 鉴别器以及 FileEntry 的 Content。
        /// </summary>
        public override void Write(Utf8JsonWriter writer, WorkspaceEntry value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString("Kind", value is DirEntry ? "dir" : "file");
            writer.WriteString("Path", value.Path);
            writer.WriteBoolean("Ephemeral", value.Ephemeral);
            if (value is FileEntry fe)
                writer.WriteString("Content", fe.Content);
            writer.WriteEndObject();
        }
    }
}
