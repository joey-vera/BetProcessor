using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BetProcessor.Tests;


public class SwaggerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SwaggerTests(WebApplicationFactory<Program> factory)
    {
        // Force Development environment so Swagger branch is active
        var factoryWithEnv = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Development");
        });

        _client = factoryWithEnv.CreateClient();
    }

    [Fact]
    public async Task SwaggerJson_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"openapi\"", content);
    }

    [Fact]
    public async Task SwaggerUI_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/swagger/index.html");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Swagger UI", content);
    }
}