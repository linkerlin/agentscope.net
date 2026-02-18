// Copyright 2024-2026 the original author or authors.
// Licensed under the Apache License, Version 2.0

using AgentScope.Core.Interruption;
using Xunit;

namespace AgentScope.Core.Tests.Interruption;

public class CancellationManagerTests
{
    [Fact]
    public void Constructor_CreatesEmptyManager()
    {
        // Arrange & Act
        using var manager = new CancellationManager();

        // Assert
        Assert.Empty(manager.GetActiveOperationIds());
    }

    [Fact]
    public void CreateScope_ReturnsValidScope()
    {
        // Arrange
        using var manager = new CancellationManager();

        // Act
        using var scope = manager.CreateScope("test-op");

        // Assert
        Assert.NotNull(scope);
        Assert.Equal("test-op", scope.OperationId);
        Assert.False(scope.IsCancellationRequested);
    }

    [Fact]
    public void GetToken_ExistingScope_ReturnsToken()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope = manager.CreateScope("test-op");

        // Act
        var token = manager.GetToken("test-op");

        // Assert
        Assert.NotEqual(default, token);
    }

    [Fact]
    public void GetToken_NonExistingScope_ThrowsException()
    {
        // Arrange
        using var manager = new CancellationManager();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => manager.GetToken("nonexistent"));
    }

    [Fact]
    public void TryGetToken_ExistingScope_ReturnsTrue()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope = manager.CreateScope("test-op");

        // Act
        var result = manager.TryGetToken("test-op", out var token);

        // Assert
        Assert.True(result);
        Assert.NotEqual(default, token);
    }

    [Fact]
    public void TryGetToken_NonExistingScope_ReturnsFalse()
    {
        // Arrange
        using var manager = new CancellationManager();

        // Act
        var result = manager.TryGetToken("nonexistent", out var token);

        // Assert
        Assert.False(result);
        Assert.Equal(default, token);
    }

    [Fact]
    public async Task CancelAsync_SetsCancellationRequested()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope = manager.CreateScope("test-op");

        // Act
        await manager.CancelAsync("test-op");

        // Assert
        Assert.True(scope.IsCancellationRequested);
    }

    [Fact]
    public async Task CancelAsync_SavesInterruptionContext()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope = manager.CreateScope("test-op");

        // Act
        await manager.CancelAsync("test-op", InterruptionReason.Timeout, "Test timeout");

        // Assert
        var context = manager.GetInterruptionContext("test-op");
        Assert.NotNull(context);
        Assert.Equal(InterruptionReason.Timeout, context.Reason);
        Assert.Equal("Test timeout", context.Message);
    }

    [Fact]
    public async Task IsCancelled_AfterCancel_ReturnsTrue()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope = manager.CreateScope("test-op");

        // Act
        await manager.CancelAsync("test-op");

        // Assert
        Assert.True(manager.IsCancelled("test-op"));
    }

    [Fact]
    public void IsCancelled_WithoutCancel_ReturnsFalse()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope = manager.CreateScope("test-op");

        // Assert
        Assert.False(manager.IsCancelled("test-op"));
    }

    [Fact]
    public void SaveState_StoresState()
    {
        // Arrange
        using var manager = new CancellationManager();
        var state = new InterruptionState
        {
            Id = "test-state",
            OperationType = "TestOperation",
            Progress = 50
        };

        // Act
        manager.SaveState("test-op", state);

        // Assert
        var saved = manager.GetSavedState("test-op");
        Assert.NotNull(saved);
        Assert.Equal("test-state", saved.Id);
        Assert.Equal(50, saved.Progress);
    }

    [Fact]
    public void RemoveSavedState_RemovesState()
    {
        // Arrange
        using var manager = new CancellationManager();
        var state = new InterruptionState
        {
            Id = "test-state",
            OperationType = "TestOperation"
        };
        manager.SaveState("test-op", state);

        // Act
        var result = manager.RemoveSavedState("test-op");

        // Assert
        Assert.True(result);
        Assert.Null(manager.GetSavedState("test-op"));
    }

    [Fact]
    public void Cleanup_RemovesToken()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope = manager.CreateScope("test-op");
        Assert.Single(manager.GetActiveOperationIds());

        // Act
        manager.Cleanup("test-op");

        // Assert
        Assert.Empty(manager.GetActiveOperationIds());
    }

    [Fact]
    public async Task CancelAll_CancelsAllOperations()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope1 = manager.CreateScope("op1");
        using var scope2 = manager.CreateScope("op2");

        // Act
        await manager.CancelAllAsync();

        // Assert
        Assert.True(scope1.IsCancellationRequested);
        Assert.True(scope2.IsCancellationRequested);
    }

    [Fact]
    public async Task Scope_ThrowIfCancellationRequested_ThrowsWhenCancelled()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope = manager.CreateScope("test-op");
        await manager.CancelAsync("test-op");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => Task.Run(() => scope.ThrowIfCancellationRequested()));
    }

    [Fact]
    public async Task Scope_Register_CallsCallbackOnCancel()
    {
        // Arrange
        using var manager = new CancellationManager();
        using var scope = manager.CreateScope("test-op");
        var callbackCalled = false;
        scope.Register(() => callbackCalled = true);

        // Act
        await manager.CancelAsync("test-op");
        await Task.Delay(100); // Give time for callback

        // Assert
        Assert.True(callbackCalled);
    }
}

public class CancellationHelperTests
{
    [Fact]
    public void CheckCancellation_WhenNotCancelled_DoesNotThrow()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act & Assert
        CancellationHelper.CheckCancellation(cts.Token);
    }

    [Fact]
    public void CheckCancellation_WhenCancelled_ThrowsException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.Throws<OperationCanceledException>(() => CancellationHelper.CheckCancellation(cts.Token));
    }

    [Fact]
    public async Task CreateTimeoutToken_AfterTimeout_IsCancelled()
    {
        // Arrange & Act
        var token = CancellationHelper.CreateTimeoutToken(TimeSpan.FromMilliseconds(100));
        
        // Wait for timeout with some buffer
        await Task.Delay(500);

        // Assert
        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void CombineTokens_WhenOneCancelled_IsCancelled()
    {
        // Arrange
        using var cts1 = new CancellationTokenSource();
        using var cts2 = new CancellationTokenSource();
        var combined = CancellationHelper.CombineTokens(cts1.Token, cts2.Token);

        // Act
        cts1.Cancel();

        // Assert
        Assert.True(combined.IsCancellationRequested);
    }
}
