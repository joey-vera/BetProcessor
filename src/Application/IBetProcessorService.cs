using Domain;

namespace Application;

public interface IBetProcessorService
{
    Task ProcessAsync(Bet bet, CancellationToken ct);
    Task WhenIdleAsync();
    string GetSummary();
    void ForceIdle();
}