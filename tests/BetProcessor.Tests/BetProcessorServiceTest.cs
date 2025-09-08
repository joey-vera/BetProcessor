using Application.Services;
using BetProcessor.Tests.Mocks;
using Domain;
using Domain.Enums;
using Microsoft.Extensions.Options;

namespace BetProcessor.Tests;

public class BetProcessorServiceTest
{
    [Fact]
    public async Task ProcessAsync_ShouldEnqueueToReview_WhenStatusInvalid()
    {
        // Arrange
        var opts = Options.Create(new WorkerOptions
        {
            WorkerCount = 2,
            ChannelCapacity = 500,
            ProcessingDelayMs = 50
        });

        var processor = new BetProcessorService(new FakeDelayService(),
            opts);

        // Bet with invalid status sequence
        var invalidBet = new Bet
        {
            Id = 1,
            Amount = 10,
            Odds = 2,
            Client = "clientX",
            Event = "event1",
            Market = "mkt",
            Selection = "sel",
            Status = BetStatus.VOID // Invalid as first status
        };

        // Act
        await processor.ProcessAsync(invalidBet, CancellationToken.None);

        // Assert
        var summary = processor.GetSummary();
        Assert.Contains("\"ReviewQueueSize\": 1", summary);
    }


    [Fact]
    public async Task ProcessAsync_ShouldEnqueueToReview_WhenOpenTwice()
    {
        // Arrange
        var opts = Options.Create(new WorkerOptions
        {
            WorkerCount = 2,
            ChannelCapacity = 500,
            ProcessingDelayMs = 50
        });

        var processor = new BetProcessorService(new FakeDelayService(),
            opts);

        // Bet with invalid status sequence
        var invalidBet = new Bet
        {
            Id = 1,
            Amount = 10,
            Odds = 2,
            Client = "clientX",
            Event = "event1",
            Market = "mkt",
            Selection = "sel",
            Status = BetStatus.OPEN // Invalid as first status
        };

        // Act
        await processor.ProcessAsync(invalidBet, CancellationToken.None);

        // Process OPEN again
        await processor.ProcessAsync(invalidBet, CancellationToken.None);

        // Assert
        var summary = processor.GetSummary();
        Assert.Contains("\"ReviewQueueSize\": 1", summary);
    }
}