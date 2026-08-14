namespace AgentScope.Core.A2A.Server.Transport;

/// <summary>
/// 传输协议包装器。对标 Java TransportWrapper。
/// 支持 JSON-RPC、gRPC、REST 等多种传输。
/// </summary>
public interface ITransportWrapper
{
    string TransportType { get; }
    Task<object> HandleRequestAsync(string body, IDictionary<string, string>? headers = null, CancellationToken ct = default);
}
