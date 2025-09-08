using Application;
using Application.Services;
using BetProcessorAPI.Endpoints;
using Microsoft.OpenApi.Models;
using System.Xml.XPath;

namespace BetProcessorAPI;

public static class DependencyInjection
{
    public static void AddServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddSingleton<IBetProcessorService, BetProcessorService>();
        services.AddSingleton<IDelayService, DelayService>();
        services.AddSingleton<BetQueueService>();
        services.AddHostedService<WorkerService>();
        services.AddEndpointsApiExplorer();
        services.AddSwagger(configuration);
        services.AddHttpContextAccessor();
    }

    public static void UseApi(this WebApplication app, ConfigurationManager configuration)
    {
        BetProcessorEndPoints.DefineEndpoints(app);
    }

    private static void AddSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        const string version = "v1";
        services.AddSwaggerGen(option =>
        {
            option.UseInlineDefinitionsForEnums();

            option.SwaggerDoc(
                version,
                new OpenApiInfo
                {
                    Title = "Bet Processor API",
                    Version = version
                });

            var xmlFiles = new[]
            {
                Path.Combine(AppContext.BaseDirectory, $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml"),
                Path.Combine(AppContext.BaseDirectory, "Domain.xml")
            };

            // Set static property for EnumSchemaFilter
            EnumSchemaFilter.XmlDocs = xmlFiles
                .Where(File.Exists)
                .Select(path => new XPathDocument(path))
                .ToList();

            foreach (var xml in xmlFiles)
                if (File.Exists(xml))
                    option.IncludeXmlComments(xml);

            // Register the filter (no arguments)
            option.SchemaFilter<EnumSchemaFilter>();
        });
    }
}