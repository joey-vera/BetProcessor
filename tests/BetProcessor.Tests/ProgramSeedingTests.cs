using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace BetProcessor.Tests;

public class ProgramSeedingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ProgramSeedingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SeededApp_ShouldHaveProcessedBets()
    {
        // Give some time to process
        await Task.Delay(3000);

        // Act
        var resp = await _client.GetAsync("/summary");

        // Assert
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadAsStringAsync();

        // The seeded 100 bets should appear in summary
        Assert.Contains("TotalProcessed\": 100", json);
    }
}