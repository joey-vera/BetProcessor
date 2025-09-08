using Application;
using Application.Services;
using Domain;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BetProcessor.Tests;

public class WorkerServiceTests
{
    [Fact]
    public async Task Worker_ShouldStop_WhenCancellationRequested()
    {
        // Arrange
        var opts = Options.Create(new WorkerOptions { WorkerCount = 1, ChannelCapacity = 10, ProcessingDelayMs = 1 });
        var queue = new BetQueueService(opts);

        var fakeProcessor = new FakeProcessorService();
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WorkerService>();

        var service = new WorkerService(queue, fakeProcessor, logger, opts);

        using var cts = new CancellationTokenSource();
        var runTask = service.StartAsync(cts.Token);

        // Act
        cts.Cancel(); // simulate shutdown
        await Task.WhenAny(runTask, Task.Delay(500));

        // Assert
        Assert.True(runTask.IsCompleted); // worker stopped gracefully
    }

    [Fact]
    public async Task Worker_ShouldCatchException_AndContinue()
    {
        // Arrange
        var opts = Options.Create(new WorkerOptions { WorkerCount = 1, ChannelCapacity = 10, ProcessingDelayMs = 1 });
        var queue = new BetQueueService(opts);

        var throwingProcessor = new ThrowingProcessorService();
        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger<WorkerService>();

        var service = new WorkerService(queue, throwingProcessor, logger, opts);

        using var cts = new CancellationTokenSource();

        // Put one bet in queue
        await queue.TryEnqueueAsync(new Bet { Id = 1, Amount = 10, Odds = 2, Client = "c1", Event = "e", Market = "m", Selection = "s", Status = BetStatus.OPEN }, cts.Token);

        var runTask = service.StartAsync(cts.Token);

        // Act
        await Task.Delay(100); // give worker time to process
        cts.Cancel();
        await Task.WhenAny(runTask, Task.Delay(500));

        // Assert
        Assert.True(runTask.IsCompleted); // worker recovered and stopped
    }

    private class FakeProcessorService : IBetProcessorService
    {
        public Task ProcessAsync(Bet bet, CancellationToken ct) => Task.CompletedTask;
        public Task WhenIdleAsync() => Task.CompletedTask;
        public string GetSummary() => "{}";

        public void ForceIdle()
        {
            throw new NotImplementedException();
        }
    }

    private class ThrowingProcessorService : IBetProcessorService
    {
        public Task ProcessAsync(Bet bet, CancellationToken ct) => throw new System.Exception("Test failure");
        public Task WhenIdleAsync() => Task.CompletedTask;
        public string GetSummary() => "{}";

        public void ForceIdle()
        {
            throw new NotImplementedException();
        }
    }
}