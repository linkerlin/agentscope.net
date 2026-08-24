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

/// <summary>远程快照工厂</summary>
public sealed class RemoteSnapshotSpec : ISandboxSnapshotSpec
{
    private readonly IRemoteSnapshotClient _client;

    public RemoteSnapshotSpec(IRemoteSnapshotClient client)
    {
        _client = client;
    }

    public ISandboxSnapshot Build(string snapshotId)
        => new RemoteSandboxSnapshot(_client, snapshotId);
}
