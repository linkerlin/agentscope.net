namespace AgentScope.Extensions.Vector;

/// <summary>
/// 向量搜索命中结果。对标 Java SearchDocumentDto。
/// </summary>
public readonly record struct SearchHit(
    string Id,
    float Score,
    IReadOnlyDictionary<string, object>? Payload = null);
