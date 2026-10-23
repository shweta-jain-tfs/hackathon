using System.Reflection;
using System.Text;
using NJsonSchema;
using NJsonSchema.Generation;
using NJsonSchema.Generation.TypeMappers;
using NSwag;
using NSwag.Generation.Processors.Security;
using Pharos.Contracts.Security;
using Pharos.Server.Swagger;

#pragma warning disable IDE0130
namespace Pharos.Server;

internal static class SwaggerExtensions
{
    public static IServiceCollection AddOpenApiDocument(this IServiceCollection services)
    {
        // Register the Swagger services
        services.AddOpenApiDocument((configure, provider) =>
        {
            configure.Title = "Pharos Web API";

            configure.SchemaSettings.SchemaNameGenerator = new CustomSchemaNameGenerator();
            configure.SchemaSettings.TypeMappers =
                new List<ITypeMapper> {
                    new ObjectTypeMapper(typeof(Encoding), new JsonSchema { Type = JsonObjectType.String })
                };

            configure.AddSecurity("Custom", Array.Empty<string>(), new OpenApiSecurityScheme
            {
                Type = OpenApiSecuritySchemeType.OAuth2,
                Name = "Authorization",
                Flow = OpenApiOAuth2Flow.Implicit,
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = $"{AccountRoutes.Login}?returnUrl=/swagger"
                    }
                }
            });

            configure.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Custom"));

#if DEBUG
            // Add permission documentation processor (DEBUG only)
            var logger = provider.GetRequiredService<ILogger<PermissionOperationProcessor>>();
            configure.OperationProcessors.Add(new PermissionOperationProcessor(logger));
#endif
            configure.OperationProcessors.Add(new RouteTagOperationProcessor());

            // Include XML comments if available
            try
            {
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    configure.SchemaSettings.GenerateXmlObjects = true;
                }
            }
            catch
            {
                // If XML file handling fails, continue without it
            }
        });

        return services;
    }
}

internal class CustomSchemaNameGenerator : ISchemaNameGenerator
{
    public string Generate(Type type)
    {
        var typeFullName = type.FullName ?? type.Name;
        return typeFullName.EndsWith("Dto") ? typeFullName.Replace("Dto", "") : typeFullName;
    }
}
