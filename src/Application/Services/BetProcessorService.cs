using Domain;
using Domain.Enums;
using System.Collections.Concurrent;

namespace Application.Services;

/// <summary>
/// Provides functionality for processing bets, tracking their statuses, and maintaining statistics about processed bets
/// and clients.
/// </summary>
/// <remarks>This service is designed to handle bet processing asynchronously, ensuring thread safety and
/// maintaining internal statistics such as total processed bets, total amounts, and profit/loss calculations. It also
/// validates bet status transitions and queues invalid bets for review.  The service supports operations such as
/// processing individual bets, waiting for all ongoing processing to complete, and generating a summary of processed
/// data.</remarks>
public class BetProcessorService : IBetProcessorService
{
    private readonly ConcurrentDictionary<int, BetStatus> _statusByBet = new();
    private readonly ConcurrentDictionary<string, ClientStats> _clientStats = new();

    private long _totalProcessed = 0;
    private double _totalAmount = 0.0;
    private double _totalPnL = 0.0;

    private readonly ConcurrentQueue<BetForReview> _review = new();

    private readonly IDelayService _delayService;
    private readonly Microsoft.Extensions.Options.IOptions<WorkerOptions> _options;

    private readonly object _lock = new();
    private int _onGoingBetsNumber = 0;
    private TaskCompletionSource _idleTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public BetProcessorService(IDelayService delayService, Microsoft.Extensions.Options.IOptions<WorkerOptions> options)
    {
        _delayService = delayService;
        _options = options;
    }

    /// <summary>
    /// Process a bet asynchronously.
    /// </summary>
    public async Task ProcessAsync(Bet bet, CancellationToken ct)
    {
        OnBetStarted();

        try
        {
            await _delayService.DelayAsync(_options.Value.ProcessingDelayMs, ct);

            if (!_statusByBet.TryGetValue(bet.Id, out var prev))
            {
                if (bet.Status != BetStatus.OPEN)
                {
                    _review.Enqueue(new BetForReview(bet.Id, bet.Status, "First status must be OPEN", DateTimeOffset.UtcNow));
                    RecordBasic(bet);
                    return;
                }
                _statusByBet[bet.Id] = bet.Status;
                RecordBasic(bet);
                return;
            }
            else
            {
                bool ok = prev == BetStatus.OPEN &&
                          (bet.Status == BetStatus.WINNER || bet.Status == BetStatus.LOSER || bet.Status == BetStatus.VOID);

                if (!ok)
                {
                    _review.Enqueue(new BetForReview(bet.Id, bet.Status, $"Invalid transition from {prev} to {bet.Status}", DateTimeOffset.UtcNow));
                    RecordBasic(bet);
                    return;
                }

                _statusByBet[bet.Id] = bet.Status;

                double pnl = bet.Status switch
                {
                    BetStatus.WINNER => bet.Amount * (bet.Odds - 1.0),
                    BetStatus.LOSER => -bet.Amount,
                    BetStatus.VOID => 0.0,
                    _ => 0.0
                };

                Interlocked.Increment(ref _totalProcessed);

                AddDouble(ref _totalAmount, bet.Amount);
                AddDouble(ref _totalPnL, pnl);

                var stats = _clientStats.GetOrAdd(bet.Client, _ => new ClientStats());
                if (pnl >= 0) stats.AddProfit(pnl); else stats.AddLoss(-pnl);
            }
        }
        finally
        {
            OnBetCompleted();
        }
    }

    /// <summary>
    /// Await until the processor has no ongoing processing bets.
    /// </summary>
    public Task WhenIdleAsync() => _idleTcs.Task;

    /// <summary>
    /// Returns a JSON summary of all processed bets.
    /// </summary>
    public string GetSummary()
    {
        var topProfit = _clientStats.Select(kvp => new { Client = kvp.Key, Profit = kvp.Value.TotalProfit })
                                .OrderByDescending(x => x.Profit)
                                .Take(5)
                                .ToList();

        var topLoss = _clientStats.Select(kvp => new { Client = kvp.Key, Loss = kvp.Value.TotalLoss })
                              .OrderByDescending(x => x.Loss)
                              .Take(5)
                              .ToList();

        var summary = new
        {
            TotalProcessed = Interlocked.Read(ref _totalProcessed),
            TotalAmount = ReadDouble(ref _totalAmount).ToString("F2"), // Format as fixed-point
            TotalProfitOrLoss = ReadDouble(ref _totalPnL).ToString("F2"), // Format as fixed-point
            Top5ClientsByProfit = topProfit,
            Top5ClientsByLoss = topLoss,
            ReviewQueueSize = _review.Count
        };

        return System.Text.Json.JsonSerializer.Serialize(summary, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public void ForceIdle()
    {
        lock (_lock)
        {
            if (_onGoingBetsNumber == 0)
            {
                _idleTcs.TrySetResult();
            }
        }
    }

    /// <summary>
    /// Called when a bet starts processing. Ensures _idleTcs is reset if we were idle.
    /// </summary>
    private void OnBetStarted()
    {
        lock (_lock)
        {
            _onGoingBetsNumber++;
            if (_onGoingBetsNumber == 1)
            {
                // System was idle, reset TCS
                _idleTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }
    }

    /// <summary>
    /// Called when a bet completes processing. Completes _idleTcs if system becomes idle.
    /// </summary>
    private void OnBetCompleted()
    {
        lock (_lock)
        {
            _onGoingBetsNumber--;
            if (_onGoingBetsNumber == 0)
            {
                _idleTcs.TrySetResult();
            }
        }
    }

    private void RecordBasic(Bet bet)
    {
        Interlocked.Increment(ref _totalProcessed);
    }

    private static void AddDouble(ref double target, double value)
    {
        double initial, computed;
        do
        {
            initial = target;
            computed = initial + value;
        }
        while (Interlocked.CompareExchange(ref target, computed, initial) != initial);
    }

    private static double ReadDouble(ref double target)
    {
        // Volatile read ensures visibility across threads
        return Volatile.Read(ref target);
    }
}