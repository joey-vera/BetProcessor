using Application;
using Application.Services;
using BetProcessor.Tests.Mocks;
using Domain;
using Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Text.Json;

namespace BetProcessor.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            // Remove existing registrations
            var processorDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IBetProcessorService));
            if (processorDescriptor != null) services.Remove(processorDescriptor);

            var queueDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(BetQueueService));
            if (queueDescriptor != null) services.Remove(queueDescriptor);

            var delayDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IDelayService));
            if (delayDescriptor != null) services.Remove(delayDescriptor);

            // Register as SINGLETON for correct background processing
            services.AddSingleton<IBetProcessorService, BetProcessorService>();
            services.AddSingleton<BetQueueService>();
            services.AddSingleton<IDelayService, FakeDelayService>();

            services.AddHostedService<WorkerService>();
        });
    }
}

public class BetProcessorAPITests : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task AddBet_Open_Then_Winner_ComputesProfit()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var open = new Bet { Id = 1, Amount = 100, Odds = 2.0, Client = "c1", Event = "e", Market = "m", Selection = "s", Status = BetStatus.OPEN };
        var win = new Bet { Id = 1, Amount = 100, Odds = 2.0, Client = "c1", Event = "e", Market = "m", Selection = "s", Status = BetStatus.WINNER };

        var r1 = await client.PostAsJsonAsync("/bet", open); r1.EnsureSuccessStatusCode();
        var r2 = await client.PostAsJsonAsync("/bet", win); r2.EnsureSuccessStatusCode();

        await WaitForProcessedAsync(client, 2);

        var summaryJson = await client.GetStringAsync("/summary");
        var summaryObj = JsonSerializer.Deserialize<JsonElement>(summaryJson);
        Assert.Equal(2, summaryObj.GetProperty("TotalProcessed").GetInt32());
        Assert.Equal("100,00", summaryObj.GetProperty("TotalProfitOrLoss").GetString());
    }

    [Fact]
    public async Task Invalid_Sequence_Goes_To_Review()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var winFirst = new Bet { Id = 999, Amount = 50, Odds = 2.5, Client = "cX", Event = "e", Market = "m", Selection = "s", Status = BetStatus.WINNER };
        var r = await client.PostAsJsonAsync("/bet", winFirst); r.EnsureSuccessStatusCode();
        var summaryJson = await client.GetStringAsync("/summary");
        var summaryObj = JsonSerializer.Deserialize<JsonElement>(summaryJson);
        Assert.Equal(1, summaryObj.GetProperty("ReviewQueueSize").GetInt32());
    }

    [Fact]
    public async Task Shutdown_Returns_Summary()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateClient();

        var resp = await client.PostAsync("/shutdown", null);
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync();
        Assert.Contains("TotalProcessed", text);
    }

    private async Task WaitForProcessedAsync(HttpClient client, int expected, int timeoutMs = 2000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var summaryJson = await client.GetStringAsync("/summary");
            var summaryObj = JsonSerializer.Deserialize<JsonElement>(summaryJson);
            if (summaryObj.GetProperty("TotalProcessed").GetInt32() == expected)
                return;
            await Task.Delay(100);
        }
        throw new TimeoutException($"Timeout waiting for TotalProcessed == {expected}");
    }
}