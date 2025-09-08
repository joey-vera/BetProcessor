using BetProcessorAPI;
using Domain;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure worker settings. Can be moved to appsettings.json or environment variables as needed.
        builder.Services.Configure<WorkerOptions>(opt =>
        {
            opt.WorkerCount = Math.Max(Environment.ProcessorCount - 1, 2);
            opt.ChannelCapacity = 10_000;
            opt.ProcessingDelayMs = 50;
        });

        builder.Services.AddServices(builder.Configuration);

        var app = builder.Build();

        // Only seed data if not running in Testing environment
        if (!app.Environment.IsEnvironment("Testing"))
        {
            DataSeedGenerator.AddInitialDataSet(app);
        }

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bet Processor API v1");
                c.RoutePrefix = "swagger";
            });
        }

        app.Logger.LogInformation("Starting Bet Processor API...");

        app.UseApi(builder.Configuration);
        await app.RunAsync();
    }
}