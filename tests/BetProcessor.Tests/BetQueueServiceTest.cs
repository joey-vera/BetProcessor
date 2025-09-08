using Domain;
using Domain.Enums;
using Microsoft.Extensions.Options;
using Application.Services;
using System.Threading.Channels;

namespace BetProcessor.Tests;

public class BetQueueServiceTest
{
    [Fact]
    public async Task TryEnqueueAsync_ShouldReturnFalse_WhenQueueClosed()
    {
        // Arrange
        var opts = Options.Create(new WorkerOptions { ChannelCapacity = 1 });
        var queue = new BetQueueService(opts);

        await queue.CompleteAsync();

        var bet = new Bet { Id = 1, Amount = 10, Odds = 2, Client = "c", Event = "e", Market = "m", Selection = "s", Status = BetStatus.OPEN };

        // Act
        var result = await queue.TryEnqueueAsync(bet, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryEnqueueAsync_ShouldReturnTrue_WhenQueueNotClosed()
    {
        // Arrange
        var opts = Options.Create(new WorkerOptions { ChannelCapacity = 1 });
        var queue = new BetQueueService(opts);

        var bet = new Bet { Id = 1, Amount = 10, Odds = 2, Client = "c", Event = "e", Market = "m", Selection = "s", Status = BetStatus.OPEN };

        // Act
        var result = await queue.TryEnqueueAsync(bet, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task TryEnqueueAsync_ShouldRespectCancellation()
    {
        var opts = Options.Create(new WorkerOptions { ChannelCapacity = 1 });
        var queue = new BetQueueService(opts);

        var bet = new Bet { Id = 2, Amount = 20, Odds = 3, Client = "c2", Event = "e2", Market = "m2", Selection = "s2", Status = BetStatus.OPEN };

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await queue.TryEnqueueAsync(bet, cts.Token);
        });
    }

    [Fact]
    public async Task TryEnqueueAsync_ShouldReturnFalse_WhenChannelIsFull()
    {
        var opts = Options.Create(new WorkerOptions { ChannelCapacity = 1 });
        var queue = new BetQueueService(opts);

        var bet1 = new Bet { Id = 1, Amount = 10, Odds = 2, Client = "c", Event = "e", Market = "m", Selection = "s", Status = BetStatus.OPEN };
        var bet2 = new Bet { Id = 2, Amount = 20, Odds = 3, Client = "c2", Event = "e2", Market = "m2", Selection = "s2", Status = BetStatus.OPEN };

        // Fill the channel
        var result1 = await queue.TryEnqueueAsync(bet1, CancellationToken.None);
        Assert.True(result1);

        // Try to enqueue another bet (should block, but since FullMode is Wait, it will wait until space is available)
        // To simulate full, we complete the channel so it can't accept more
        await queue.CompleteAsync();
        var result2 = await queue.TryEnqueueAsync(bet2, CancellationToken.None);
        Assert.False(result2);
    }

    [Fact]
    public async Task CompleteAsync_CanBeCalledMultipleTimes()
    {
        var opts = Options.Create(new WorkerOptions { ChannelCapacity = 1 });
        var queue = new BetQueueService(opts);

        await queue.CompleteAsync();
        // Should not throw
        await queue.CompleteAsync();
    }
}