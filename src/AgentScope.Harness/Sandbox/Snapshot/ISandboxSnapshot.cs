namespace AgentScope.Harness.Sandbox.Snapshot;

/// <summary>沙箱快照接口：持久化和恢复沙箱工作区</summary>
public interface ISandboxSnapshot
{
    string Id { get; }
    string Type { get; }
    bool IsPersistenceEnabled { get; }

    /// <summary>持久化工作区流到快照</summary>
    Task PersistAsync(Stream data, CancellationToken ct = default);

    /// <summary>恢复快照到工作区</summary>
    Task<Stream> RestoreAsync(CancellationToken ct = default);

    /// <summary>检查快照是否可恢复</summary>
    bool IsRestorable();
}

/// <summary>快照规范工厂接口</summary>
public interface ISandboxSnapshotSpec
{
    ISandboxSnapshot Build(string snapshotId);
}
