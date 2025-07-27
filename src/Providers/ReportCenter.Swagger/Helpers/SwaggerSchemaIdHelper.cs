using System.Reflection;
using ReportCenter.Swagger.Attributes;

namespace ReportCenter.Swagger.Helpers;

public static class SwaggerSchemaIdHelper
{
    public static string GetSwaggerSchemaId(Type type)
    {
        var attribute = type.GetCustomAttribute<SwaggerSchemaIdFilterAttribute>();
        return attribute?.Name ?? type.Name;
    }
}
