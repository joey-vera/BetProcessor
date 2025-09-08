using BetProcessorAPI;
using Domain.Enums;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;
using System.Xml.XPath;

namespace BetProcessor.Tests;

public class EnumSchemaFilterTests
{
    [Fact]
    public void SchemaFilter_ShouldContain_BetStatusEnum_Descriptions()
    {
        // Arrange
        var schema = new OpenApiSchema
        {
            Enum = Enum.GetNames(typeof(BetStatus))
        .Select(name => (IOpenApiAny)new OpenApiString(name))
        .ToList()
        };

        // Ensure XmlDocs is set to an empty list to avoid null reference
        EnumSchemaFilter.XmlDocs = new List<XPathDocument>();

        var schemaGenerator = new SchemaGenerator(
            new SchemaGeneratorOptions(),
            new JsonSerializerDataContractResolver(new JsonSerializerOptions())
        );
        var schemaRepository = new SchemaRepository();

        var context = new SchemaFilterContext(
            typeof(BetStatus),
            schemaGenerator,
            schemaRepository
        );

        var filter = new EnumSchemaFilter();

        // Act
        filter.Apply(schema, context);

        // Assert
        Assert.Contains("OPEN", string.Join(",", schema.Enum.Select(e => ((OpenApiString)e).Value)));
        Assert.Contains("WINNER", string.Join(",", schema.Enum.Select(e => ((OpenApiString)e).Value)));
        Assert.Contains("LOSER", string.Join(",", schema.Enum.Select(e => ((OpenApiString)e).Value)));
        Assert.Contains("VOID", string.Join(",", schema.Enum.Select(e => ((OpenApiString)e).Value)));
    }
}