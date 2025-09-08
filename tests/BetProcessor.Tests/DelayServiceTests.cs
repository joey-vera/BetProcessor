using Application.Services;

namespace BetProcessor.Tests;

public class DelayServiceTests
{
    [Fact]
    public async Task DelayAsync_ShouldWait_WhenNotCancelled()
    {
        // Arrange
        var delay = new DelayService();
        using var cts = new CancellationTokenSource();

        var start = DateTime.UtcNow;

        // Act
        await delay.DelayAsync(50, cts.Token);

        var elapsed = DateTime.UtcNow - start;

        // Assert
        Assert.True(elapsed.TotalMilliseconds >= 45, "Delay should wait at least ~50ms");
    }

    [Fact]
    public async Task DelayAsync_ShouldThrow_WhenCancelled()
    {
        // Arrange
        var delay = new DelayService();
        using var cts = new CancellationTokenSource();

        // Act
        cts.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(() => delay.DelayAsync(100, cts.Token));
    }
}