using Application;
using Application.Services;
using Domain;

namespace BetProcessorAPI.Endpoints;

internal static class BetProcessorEndPoints
{
    public static void DefineEndpoints(WebApplication app)
    {
        app.MapPost("/bet", async (Bet bet, BetQueueService queueService) =>
        {
            if (bet == null) return Results.BadRequest("Bet data required.");
            var ok = await queueService.TryEnqueueAsync(bet, CancellationToken.None);
            return ok ? Results.Accepted($"/bet/{bet.Id}") : Results.StatusCode(429);
        }).WithTags("Bets");

        app.MapPost("/shutdown", async (BetQueueService queueService, IHostApplicationLifetime lifetime, IBetProcessorService processorService, IWebHostEnvironment env) =>
        {
            // Initiate graceful shutdown: stop accepting new, complete channel, wait for drain
            await queueService.CompleteAsync();
            await processorService.WhenIdleAsync();

            var summary = processorService.GetSummary();

            // Request the host to stop after we return response.
            // lifetime.StopApplication() should not be awaited.  
            // Using Task.Run ensures the response is sent before the shutdown begins.
            // Only stop the application if not running in Testing
            if (!env.IsEnvironment("Testing"))
            {
#pragma warning disable CS4014// Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(() => lifetime.StopApplication());
#pragma warning restore CS4014
            }

            return Results.Content(summary, "application/json");
        }).WithTags("System");

        app.MapGet("/summary", (IBetProcessorService processorService) => processorService.GetSummary())
            .WithTags("System");
    }
}