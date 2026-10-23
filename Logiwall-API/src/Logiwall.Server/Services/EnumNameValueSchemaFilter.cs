using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Logiwall.Server.Services;

public class EnumNameValueSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            var enumType = context.Type;
            var enumNames = Enum.GetNames(enumType);
            var enumValues = Enum.GetValues(enumType).Cast<int>();

            schema.Enum.Clear();

            foreach (var (name, value) in enumNames.Zip(enumValues, (n, v) => (n, v)))
            {
                // Format: "EnumName (EnumValue)"
                schema.Enum.Add(new OpenApiString($"{name} ({value})"));
            }
        }
    }
}
