namespace Application;

public interface IDelayService
{
    Task DelayAsync(int ms, CancellationToken ct);
}