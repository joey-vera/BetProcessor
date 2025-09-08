using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Xml.XPath;

namespace BetProcessorAPI;

/// <summary>
/// A schema filter that enhances the OpenAPI schema for enumerations by adding descriptions extracted from XML
/// documentation files.
/// </summary>
/// <remarks>This filter inspects the provided enumeration type and appends a detailed description of its values
/// to the schema. The descriptions are sourced from XML documentation files, which must be loaded into the <see
/// cref="XmlDocs"/> property prior to applying the filter.</remarks>
public class EnumSchemaFilter : ISchemaFilter
{
    public static List<XPathDocument> XmlDocs { get; set; } = new();

    public EnumSchemaFilter() { }

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!context.Type.IsEnum || XmlDocs.Count == 0)
            return;

        var enumType = context.Type;
        var descriptions = new List<string>();

        foreach (var name in Enum.GetNames(enumType))
        {
            var value = Convert.ToInt32(Enum.Parse(enumType, name));
            string? summary = null;

            foreach (var xml in XmlDocs)
            {
                var nav = xml.CreateNavigator();
                var fullName = $"F:{enumType.FullName}.{name}";
                var node = nav.SelectSingleNode($"/doc/members/member[@name='{fullName}']/summary");
                if (node != null)
                {
                    summary = node.InnerXml.Trim();
                    break;
                }
            }

            descriptions.Add($"- `{name}` ({value}): {summary}");
        }

        var enumDoc = string.Join("\n", descriptions);
        schema.Description = (schema.Description ?? "") + "\n\n**Values:**\n" + enumDoc;
    }
}