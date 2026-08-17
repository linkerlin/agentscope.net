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

namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>远程存储快照实现：委派给 IRemoteSnapshotClient</summary>
public sealed class RemoteSandboxSnapshot : ISandboxSnapshot
{
    private readonly IRemoteSnapshotClient? _client;

    public RemoteSandboxSnapshot(IRemoteSnapshotClient client, string snapshotId)
    {
        _client = client;
        Id = snapshotId;
    }

    public string Id { get; }
    public string Type => "remote";
    public bool IsPersistenceEnabled => true;

    public async Task PersistAsync(Stream data, CancellationToken ct = default)
    {
        var client = RequireClient();
        await client.UploadAsync(Id, data, ct);
    }

    public async Task<Stream> RestoreAsync(CancellationToken ct = default)
    {
        var client = RequireClient();
        return await client.DownloadAsync(Id, ct);
    }

    public async Task<bool> IsRestorableAsync(CancellationToken ct = default)
    {
        var client = RequireClient();
        return await client.ExistsAsync(Id, ct);
    }

    public bool IsRestorable() => true;

    private IRemoteSnapshotClient RequireClient()
        => _client ?? throw new InvalidOperationException(
            "RemoteSnapshotClient is required for remote snapshot operations");
}
