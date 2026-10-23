using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Pharos.Server.Swagger
{
    /// <summary>
    /// Groups Swagger operations by route segment instead of controller name.
    /// </summary>
    public sealed class RouteTagOperationProcessor : IOperationProcessor
    {
        public bool Process(OperationProcessorContext context)
        {
            // Example path from NSwag: "/api/requests/tubeHistory"
            var path = context.OperationDescription.Path?.Trim('/') ?? string.Empty;

            var segments = path
                .Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Example strategies:
            // - group by 2nd segment ("requests") if you have a common "api" prefix
            // - fall back to first segment if only one exists
            var groupName =
                segments.Length > 1 ? segments[1] :
                segments.Length == 1 ? segments[0] :
                "default";

            context.OperationDescription.Operation.Tags.Clear();
            context.OperationDescription.Operation.Tags.Add(groupName);

            return true; // keep the operation
        }
    }
}
