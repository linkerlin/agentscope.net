// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Tracing;
using Xunit;

namespace AgentScope.Core.Tests.Tracing;

public class SamplerTests
{
    [Fact]
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
    public void ProbabilitySampler_Constructor_InvalidProbability_Throws(double probability)
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ProbabilitySampler(probability));
    }

    [Fact]
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
