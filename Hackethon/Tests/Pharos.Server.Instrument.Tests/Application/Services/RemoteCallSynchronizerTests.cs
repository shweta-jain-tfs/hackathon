using Microsoft.Extensions.Logging;
using Moq;

namespace Pharos.Server.Instrument.Application.Services;

public class RemoteCallSynchronizerTests
{
    private readonly RemoteCallSynchronizer _sut = new(Mock.Of<ILogger<RemoteCallSynchronizer>>());

    [Fact]
    public async Task WaitForExecutionContextAsync_ReturnsTrue_WhenLockIsAcquiredWithinTimeout()
    {
        var result = await _sut.WaitForExecutionContextAsync("INSTR-001", TimeSpan.FromMilliseconds(10));
        Assert.True(result);
        _sut.ReleaseExecutionContext("INSTR-001");
    }

    [Fact]
    public async Task WaitForExecutionContextAsync_ReturnsFalse_WhenTimeoutOccurs()
    {
        // Acquire first to block subsequent acquisition
        var acquired = await _sut.WaitForExecutionContextAsync("INSTR-002", TimeSpan.FromMilliseconds(10));
        Assert.True(acquired);

        var timedOut = await _sut.WaitForExecutionContextAsync("INSTR-002", TimeSpan.FromMilliseconds(5));
        Assert.False(timedOut);

        _sut.ReleaseExecutionContext("INSTR-002");
    }

    [Fact]
    public async Task ReleaseExecutionContext_AllowsNextWaitToAcquireAsync()
    {
        var acquired = await _sut.WaitForExecutionContextAsync("INSTR-003", TimeSpan.FromMilliseconds(10));
        Assert.True(acquired);

        _sut.ReleaseExecutionContext("INSTR-003");

        var nextAcquire = await _sut.WaitForExecutionContextAsync("INSTR-003", TimeSpan.FromMilliseconds(10));
        Assert.True(nextAcquire);
        _sut.ReleaseExecutionContext("INSTR-003");
    }

    [Fact]
    public async Task ConcurrentWaits_AllowOnlyOneHolderAtATime_Async()
    {
        var first = await _sut.WaitForExecutionContextAsync("INSTR-004", TimeSpan.FromMilliseconds(10));
        Assert.True(first);

        // Second concurrent wait should time out while first holds the lock
        var secondTask = _sut.WaitForExecutionContextAsync("INSTR-004", TimeSpan.FromMilliseconds(5));
        var second = await secondTask;
        Assert.False(second);

        _sut.ReleaseExecutionContext("INSTR-004");

        // Third should acquire after release
        var third = await _sut.WaitForExecutionContextAsync("INSTR-004", TimeSpan.FromMilliseconds(10));
        Assert.True(third);
        _sut.ReleaseExecutionContext("INSTR-004");
    }

    [Fact]
    public void ReleaseExecutionContext_IgnoresWhenNotHeld()
    {
        // Releasing on non-held semaphore should not throw
        _sut.ReleaseExecutionContext("INSTR-005");
    }

    [Fact]
    public void WaitForExecutionContext_ReturnsTrue_WhenLockIsAcquiredWithinTimeout_Sync()
    {
        var result = _sut.WaitForExecutionContext("INSTR-006", TimeSpan.FromMilliseconds(10));
        Assert.True(result);
        _sut.ReleaseExecutionContext("INSTR-006");
    }

    [Fact]
    public void WaitForExecutionContext_ReturnsFalse_WhenTimeoutOccurs_Sync()
    {
        var acquired = _sut.WaitForExecutionContext("INSTR-007", TimeSpan.FromMilliseconds(10));
        Assert.True(acquired);

        var timedOut = _sut.WaitForExecutionContext("INSTR-007", TimeSpan.FromMilliseconds(5));
        Assert.False(timedOut);

        _sut.ReleaseExecutionContext("INSTR-007");
    }

    [Fact]
    public void ReleaseExecutionContext_AllowsNextWaitToAcquire_Sync()
    {
        var acquired = _sut.WaitForExecutionContext("INSTR-008", TimeSpan.FromMilliseconds(10));
        Assert.True(acquired);

        _sut.ReleaseExecutionContext("INSTR-008");

        var nextAcquire = _sut.WaitForExecutionContext("INSTR-008", TimeSpan.FromMilliseconds(10));
        Assert.True(nextAcquire);
        _sut.ReleaseExecutionContext("INSTR-008");
    }
}
