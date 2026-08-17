// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tracing;
using Xunit;

namespace AgentScope.Core.Tests.Tracing;

/// <summary>
/// Tests for tracing samplers (AlwaysOnSampler, AlwaysOffSampler, ProbabilitySampler)
/// 跟踪采样器（AlwaysOnSampler、AlwaysOffSampler、ProbabilitySampler）的测试
/// </summary>
public class SamplerTests
{
    [Fact]
    /// <summary>
    /// AlwaysOnSampler.ShouldSample always returns true
    /// 测试 AlwaysOnSampler.ShouldSample 始终返回 true
    /// </summary>
    public void AlwaysOnSampler_ShouldSample_ReturnsTrue()
    {
        // Arrange
        var sampler = new AlwaysOnSampler();
        var context = new TraceContext
        {
            TraceId = Guid.NewGuid().ToString("N"),
            SpanId = Guid.NewGuid().ToString("N")[..16]
        };

        // Act
        var result = sampler.ShouldSample(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    /// <summary>
    /// AlwaysOffSampler.ShouldSample always returns false
    /// 测试 AlwaysOffSampler.ShouldSample 始终返回 false
    /// </summary>
    public void AlwaysOffSampler_ShouldSample_ReturnsFalse()
    {
        // Arrange
        var sampler = new AlwaysOffSampler();
        var context = new TraceContext
        {
            TraceId = Guid.NewGuid().ToString("N"),
            SpanId = Guid.NewGuid().ToString("N")[..16]
        };

        // Act
        var result = sampler.ShouldSample(context);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    /// <summary>
    /// ProbabilitySampler constructor accepts valid probability values
    /// 测试 ProbabilitySampler 构造器接受有效的概率值
    /// </summary>
    public void ProbabilitySampler_Constructor_ValidProbability(double probability)
    {
        // Arrange & Act
        var sampler = new ProbabilitySampler(probability);

        // Assert
        Assert.NotNull(sampler);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    /// <summary>
    /// ProbabilitySampler constructor throws for invalid probability values
    /// 测试 ProbabilitySampler 构造器对无效概率值抛出异常
    /// </summary>
    public void ProbabilitySampler_Constructor_InvalidProbability_Throws(double probability)
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProbabilitySampler(probability));
    }

    [Fact]
    /// <summary>
    /// ProbabilitySampler with probability 0.0 always returns false
    /// 测试 ProbabilitySampler 在概率为 0.0 时始终返回 false
    /// </summary>
    public void ProbabilitySampler_ShouldSample_ProbabilityZero_AlwaysFalse()
    {
        // Arrange
        var sampler = new ProbabilitySampler(0.0);
        var context = new TraceContext
        {
            TraceId = Guid.NewGuid().ToString("N"),
            SpanId = Guid.NewGuid().ToString("N")[..16]
        };

        // Act
        var result = sampler.ShouldSample(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    /// <summary>
    /// ProbabilitySampler with probability 1.0 always returns true
    /// 测试 ProbabilitySampler 在概率为 1.0 时始终返回 true
    /// </summary>
    public void ProbabilitySampler_ShouldSample_ProbabilityOne_AlwaysTrue()
    {
        // Arrange
        var sampler = new ProbabilitySampler(1.0);
        var context = new TraceContext
        {
            TraceId = Guid.NewGuid().ToString("N"),
            SpanId = Guid.NewGuid().ToString("N")[..16]
        };

        // Act
        var result = sampler.ShouldSample(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    /// <summary>
    /// ProbabilitySampler with probability 0.5 returns approximately 50% true over 1000 iterations
    /// 测试 ProbabilitySampler 在概率为 0.5 时，1000 次迭代中约 50% 返回 true
    /// </summary>
    public void ProbabilitySampler_ShouldSample_ProbabilityHalf_ApproximatelyHalf()
    {
        // Arrange
        var sampler = new ProbabilitySampler(0.5);
        var context = new TraceContext
        {
            TraceId = Guid.NewGuid().ToString("N"),
            SpanId = Guid.NewGuid().ToString("N")[..16]
        };

        // Act
        var results = new List<bool>();
        for (int i = 0; i < 1000; i++)
        {
            results.Add(sampler.ShouldSample(context));
        }

        // Assert - should be approximately 500 true (within 100)
        var trueCount = results.Count(r => r);
        Assert.True(trueCount >= 400 && trueCount <= 600, $"Expected approximately 500 true, got {trueCount}");
    }

    [Fact]
    /// <summary>
    /// Tracer with AlwaysOffSampler does not export spans
    /// 测试 Tracer 使用 AlwaysOffSampler 时不导出 span
    /// </summary>
    public void Tracer_WithAlwaysOffSampler_DoesNotSample()
    {
        // Arrange
        using var exporter = new InMemorySpanExporter();
        var tracer = new Tracer("test-tracer", exporter, new AlwaysOffSampler());

        // Act
        using var span = tracer.StartSpan("test-operation");
        span.End();

        // Assert
        Assert.Equal(0, exporter.Count);
    }

    [Fact]
    /// <summary>
    /// Tracer with AlwaysOnSampler exports spans
    /// 测试 Tracer 使用 AlwaysOnSampler 时导出 span
    /// </summary>
    public void Tracer_WithAlwaysOnSampler_Samples()
    {
        // Arrange
        using var exporter = new InMemorySpanExporter();
        var tracer = new Tracer("test-tracer", exporter, new AlwaysOnSampler());

        // Act
        using var span = tracer.StartSpan("test-operation");
        span.End();

        // Assert
        Assert.Equal(1, exporter.Count);
    }
}
