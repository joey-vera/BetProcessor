using Domain;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace BetProcessor.Tests;

public class ShutdownTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ShutdownTests(WebApplicationFactory<Program> factory)
    {
        // Run in Development to ensure Swagger & full middleware pipeline
        var factoryWithEnv = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
        });

        _client = factoryWithEnv.CreateClient();
    }

    [Fact]
    public async Task Shutdown_ShouldReturnSummary_AndStopApp()
    {
        // Arrange: enqueue a bet so processor does some work
        var bet = new Bet
        {
            Id = 123,
            Amount = 50,
            Odds = 2.0,
            Client = "shutdown-client",
            Event = "eventX",
            Market = "mkt",
            Selection = "sel",
            Status = BetStatus.OPEN
        };

        var resp1 = await _client.PostAsJsonAsync("/bet", bet);
        Assert.Equal(HttpStatusCode.Accepted, resp1.StatusCode);

        // Act: call shutdown
        var resp2 = await _client.PostAsync("/shutdown", null);

        // Assert: summary JSON is returned
        Assert.Equal(HttpStatusCode.OK, resp2.StatusCode);
        var summaryJson = await resp2.Content.ReadAsStringAsync();
        Assert.Contains("TotalProcessed", summaryJson);
    }
}