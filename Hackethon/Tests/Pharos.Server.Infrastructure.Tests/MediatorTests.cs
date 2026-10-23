using System.Reflection;
using MediatR;
using Pharos.Server.Application;

namespace Pharos.Server.Infrastructure.Tests;

public class MediatorTests
{
    [Fact]
    public void NotificationHandlersMustNotInjectCurrentUserOrCurrentInstrumentServices()
    {
        var checkedHandlers = 0;
        var forbiddenDependencies = new[] { typeof(ICurrentUserService), typeof(ICurrentInstrumentService) };

        foreach (var module in Modules.ModuleList)
        {
            var handlers = module.Assembly
                .DefinedTypes
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .Where(t => t.ImplementedInterfaces.Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
                .ToList();

            foreach (var handler in handlers)
            {
                checkedHandlers++;

                var constructors = handler.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var constructor in constructors)
                {
                    var forbiddenParameter = constructor
                        .GetParameters()
                        .Select(p => p.ParameterType)
                        .FirstOrDefault(p => forbiddenDependencies.Contains(p));

                    // Notification handlers should inject neither ICurrentUserService nor ICurrentInstrumentService,
                    // as they are always executed in contexts where these services are not available (e.g., background jobs).
                    Assert.True(forbiddenParameter is null,
                        $"Notification handler {handler.FullName} injects forbidden dependency {forbiddenParameter?.Name}.");
                }
            }
        }

        Assert.True(checkedHandlers > 0, "No INotificationHandler implementations were found in module assemblies.");
    }
}
