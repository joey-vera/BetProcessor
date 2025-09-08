namespace Application.Services;

public class DelayService : IDelayService
{
    public Task DelayAsync(int ms, CancellationToken ct) => Task.Delay(ms, ct);
}