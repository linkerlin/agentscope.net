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
    /// <summary>
    /// 注册 AgentScope OpenTelemetry 追踪服务到 DI 容器
    /// Registers AgentScope OpenTelemetry tracing services into the DI container
    /// </summary>
    /// <param name="services">服务集合 / Service collection</param>
    /// <param name="configure">可选的 OtelTracingOptions 配置委托 / Optional OtelTracingOptions configuration delegate</param>
    /// <returns>服务集合 / Service collection</returns>
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

/// <summary>
/// OpenTelemetry 追踪配置选项
/// OpenTelemetry tracing configuration options
/// </summary>
public sealed class OtelTracingOptions
{
    /// <summary>
    /// 是否启用 OTLP 导出器（默认启用）
    /// Whether to enable the OTLP exporter (enabled by default)
    /// </summary>
    public bool EnableOtlp { get; set; } = true;

    /// <summary>
    /// OTLP 接收端点地址
    /// OTLP receiver endpoint address
    /// </summary>
    public string? OtlpEndpoint { get; set; }

    /// <summary>
    /// 是否使用 gRPC 协议（默认启用，否则使用 HTTP Protobuf）
    /// Whether to use gRPC protocol (enabled by default, otherwise HTTP Protobuf)
    /// </summary>
    public bool UseGrpc { get; set; } = true;

    /// <summary>
    /// 是否启用控制台导出器（用于调试）
    /// Whether to enable the console exporter (for debugging)
    /// </summary>
    public bool EnableConsole { get; set; }
}

/// <summary>
/// 内部 OTel 追踪配置记录 - 包装 OtelTracingOptions 用于 DI 注册
/// Internal OTel tracing configuration record - wraps OtelTracingOptions for DI registration
/// </summary>
/// <param name="Options">追踪配置选项 / Tracing configuration options</param>
internal sealed record OtelTracingConfig(OtelTracingOptions Options);
