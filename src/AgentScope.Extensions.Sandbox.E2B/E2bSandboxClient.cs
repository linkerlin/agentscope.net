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

namespace AgentScope.Extensions.Sandbox.E2B;

/// <summary>
/// E2B 沙箱客户端（薄工厂）。对标 Java E2bSandboxClient。
/// 负责从 Options/State 构造 <see cref="E2BSandbox"/> 实例，并做状态 JSON 序列化。
/// </summary>
public sealed class E2bSandboxClient : ISandboxClient
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new WorkspaceEntryJsonConverter() },
    };

    private readonly E2bSandboxClientOptions _options;
    private readonly HttpClient _http;

    public E2bSandboxClient(E2bSandboxClientOptions? options = null, HttpClient? http = null)
    {
        _options = options ?? new E2bSandboxClientOptions();
        _http = http ?? new HttpClient();
    }

    public Task<ISandbox> CreateAsync(WorkspaceSpec spec, CancellationToken ct = default)
    {
        var sandbox = new E2BSandbox(_http, _options.ApiKey, _options.ApiBaseUrl);
        return Task.FromResult<ISandbox>(sandbox);
    }

    public Task<ISandbox> ResumeAsync(SandboxState state, CancellationToken ct = default)
    {
        var s = E2bSandboxState.FromSandboxState(state);
        var sandbox = new E2BSandbox(_http, _options.ApiKey, _options.ApiBaseUrl);
        return Task.FromResult<ISandbox>(sandbox);
    }

    public async Task DeleteAsync(ISandbox sandbox, CancellationToken ct = default)
    {
        if (sandbox == null) return;
        try { await sandbox.StopAsync(ct).ConfigureAwait(false); } catch { }
        try { await sandbox.DisposeAsync().ConfigureAwait(false); } catch { }
    }

    public string SerializeState(SandboxState state) => JsonSerializer.Serialize(state, JsonOpts);

    public SandboxState DeserializeState(string json)
        => JsonSerializer.Deserialize<SandboxState>(json, JsonOpts)
           ?? throw new InvalidOperationException("Failed to deserialize E2B sandbox state.");

    /// <summary>处理 <see cref="WorkspaceEntry"/> 多态序列化（FileEntry/DirEntry）。</summary>
    private sealed class WorkspaceEntryJsonConverter : JsonConverter<WorkspaceEntry>
    {
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
