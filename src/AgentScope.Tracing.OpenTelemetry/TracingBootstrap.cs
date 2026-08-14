using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Trace;

// 解决命名空间冲突
using OtlpExportProtocol = OpenTelemetry.Exporter.OtlpExportProtocol;

namespace AgentScope.Tracing.OpenTelemetry;

/// <summary>
/// Tracing 引导程序。提供 AddAgentScopeTracing() DI 扩展方法。
/// 对标 Java Studio TelemetryTracer + aistio tracing.go。
/// </summary>
public static class TracingBootstrap
{
    public static IServiceCollection AddAgentScopeTracing(
        this IServiceCollection services,
        Action<OtelTracingOptions>? configure = null)
    {
        var options = new OtelTracingOptions();
        configure?.Invoke(options);

        services.TryAddSingleton<OtelTracingMiddleware>();
        services.TryAddSingleton(new OtelTracingConfig(options));

        services.AddOpenTelemetry()
            .WithTracing(builder =>
            {
                builder.AddSource("io.agentscope");

                if (options.EnableOtlp && options.OtlpEndpoint != null)
                {
                    builder.AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = new Uri(options.OtlpEndpoint);
                        opt.Protocol = options.UseGrpc
                            ? OtlpExportProtocol.Grpc
                            : OtlpExportProtocol.HttpProtobuf;
                    });
                }

                if (options.EnableConsole)
                {
                    builder.AddConsoleExporter();
                }
            });

        return services;
    }
}

public sealed class OtelTracingOptions
{
    public bool EnableOtlp { get; set; } = true;
    public string? OtlpEndpoint { get; set; }
    public bool UseGrpc { get; set; } = true;
    public bool EnableConsole { get; set; }
}

internal sealed record OtelTracingConfig(OtelTracingOptions Options);
