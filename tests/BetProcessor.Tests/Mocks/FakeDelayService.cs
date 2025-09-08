using Application;

namespace BetProcessor.Tests.Mocks;

public class FakeDelayService : IDelayService
{
    public Task DelayAsync(int ms, CancellationToken ct) => Task.CompletedTask;
}