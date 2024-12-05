using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using MyBGList.Attributes;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MyBGList.Swagger;

public class SortOrderFilter : IParameterFilter
{
    public void Apply(OpenApiParameter parameter, ParameterFilterContext context)
    {
        // get all SortOrderValidatorAttribute attributes
        var attributes = context.ParameterInfo
            .GetCustomAttributes(true)
            .Union(
                context.ParameterInfo.ParameterType.GetProperties()
                    .Where(x => x.Name == parameter.Name)
                    .SelectMany(x => x.GetCustomAttributes(true))
            )
            .OfType<SortOrderValidatorAttribute>();

        if (attributes != null)
        {
            foreach (var attribute in attributes)
            {
                // add new key-value pair to the swagger parameter schema
                parameter.Schema.Extensions.Add(
                    "pattern",
                    new OpenApiString(
                        string.Join(
                            '|', 
                            attribute.AllowedValues.Select(v => $"^{v}$")
                        )
                    )
                );
            }
        }
    }
}