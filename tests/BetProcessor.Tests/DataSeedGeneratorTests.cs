using Application.Services;
using BetProcessor.Tests.Mocks;
using BetProcessorAPI;
using Domain;
using Microsoft.Extensions.Options;

namespace BetProcessor.Tests;

public class DataSeedGeneratorTests
{
    [Fact]
    public async Task SeedBetsAsync_ShouldEnqueue100Bets()
    {
        // Arrange
        var opts = Options.Create(new WorkerOptions
        {
            WorkerCount = 2,
            ChannelCapacity = 500,
            ProcessingDelayMs = 1
        });

        var queue = new BetQueueService(opts);
        var processor = new BetProcessorService(new FakeDelayService(), opts);

        // Act
        await DataSeedGenerator.SeedBetsAsync(queue, processor);
        await queue.CompleteAsync(); // <-- This signals no more bets will be written

        // Assert
        int count = 0;
        await foreach (var bet in queue.Reader.ReadAllAsync(CancellationToken.None))
        {
            count++;
        }

        Assert.Equal(100, count);
    }
}