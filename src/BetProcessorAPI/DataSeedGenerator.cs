using Application;
using Application.Services;
using Domain;
using Domain.Enums;

namespace BetProcessorAPI;

public static class DataSeedGenerator
{
    public static void AddInitialDataSet(WebApplication app)
    {
        // Seed initial 100 bets after app starts
        app.Lifetime.ApplicationStarted.Register(async () =>
        {
            var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
            using var scope = scopeFactory.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<BetQueueService>();
            var processor = scope.ServiceProvider.GetRequiredService<IBetProcessorService>();

            await SeedBetsAsync(queue, processor);
        });
    }

    public static async Task SeedBetsAsync(BetQueueService queue, IBetProcessorService processor)
    {
        var rnd = new Random(42);

        // Some malformed updates to exercise review queue
        var malformedBetNumber = 2; // must be even number to keep pairs of OPEN + closing status adds a total of 100 bets

        // Create a list of bets to ensure data is preserved from OPEN to closing status.
        var existingBets = new List<Bet>();

        // Generate 50 unique bet IDs, then push OPEN followed by a closing status later
        var ids = Enumerable.Range(1, 50 - malformedBetNumber / 2).ToArray();
        foreach (var id in ids)
        {
            var bet = new Bet
            {
                Id = id,
                Amount = Math.Round(rnd.Next(5, 200) + rnd.NextDouble(), 2),
                Odds = Math.Round(1.1 + rnd.NextDouble() * 5.0, 2),
                Client = $"client-{rnd.Next(1, 15)}",
                Event = $"event-{rnd.Next(1, 10)}",
                Market = $"market-{rnd.Next(1, 10)}",
                Selection = new[] { "HOME", "AWAY", "DRAW" }[rnd.Next(0, 3)],
                Status = BetStatus.OPEN
            };

            existingBets.Add(bet);

            await queue.TryEnqueueAsync(bet, CancellationToken.None);
        }
        // Closing updates (WINNER/LOSER/VOID) for the same IDs.
        foreach (var bet in existingBets)
        {
            var closing = new Bet
            {
                Id = bet.Id,
                Amount = bet.Amount,
                Odds = bet.Odds,
                Client = bet.Client,
                Event = bet.Event,
                Market = bet.Market,
                Selection = bet.Selection,
                Status = (BetStatus)new[] { BetStatus.WINNER, BetStatus.LOSER, BetStatus.VOID }[rnd.Next(0, 3)]
            };

            await queue.TryEnqueueAsync(closing, CancellationToken.None);
        }

        for (int i = 0; i < malformedBetNumber; i++)
        {
            await queue.TryEnqueueAsync(new Bet
            {
                Id = rnd.Next(1000, 2000),
                Amount = 50,
                Odds = 2.0,
                Client = "client-bad",
                Event = $"event-{rnd.Next(1, 10)}",
                Market = $"market-{rnd.Next(1, 10)}",
                Selection = new[] { "HOME", "AWAY", "DRAW" }[rnd.Next(0, 3)],
                Status = (BetStatus)new[] { BetStatus.WINNER, BetStatus.LOSER, BetStatus.VOID }[rnd.Next(0, 3)] // arrives without OPEN -> review
            }, CancellationToken.None);
        }
    }
}