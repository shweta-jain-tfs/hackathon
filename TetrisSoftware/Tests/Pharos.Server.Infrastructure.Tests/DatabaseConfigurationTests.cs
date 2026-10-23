using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Pharos.Server.LabCommunity.Domain;
using Pharos.Server.Lis.Domain;

namespace Pharos.Server.Infrastructure.Tests;

public class DatabaseConfigurationTests
{
    [Fact]
    public void AllStringPropertiesInDomainClassesMustHaveMaxLength()
    {
        string[] allowedPropertiesWithoutMaxLength =
        [
            $"{nameof(QueuedLisMessage)}.{nameof(QueuedLisMessage.MessageMetadata)}",
            $"{nameof(QueuedLisMessage)}.{nameof(QueuedLisMessage.MessageData)}",
            $"{nameof(ExportItem)}.{nameof(ExportItem.PayloadJson)}"
        ];

        var modules = Modules.ModuleList.Select(m => m.Assembly).ToList();

        foreach (var module in modules)
        {
            var types = module
                .GetTypes()
                .Where(x => x.IsClass && (x.FullName?.Contains(".Domain.") ?? false))
                .ToList();

            // add types from referenced "domain" assemblies
            module.GetReferencedAssemblies()
                .Where(x => x.FullName.Contains(".Domain."))
                .Select(Assembly.Load)
                .ToList()
                .ForEach(x => types.AddRange(x.GetTypes().Where(y => y.IsClass && (y.FullName?.Contains(".Domain.") ?? false))));

            foreach (var type in types)
            {
                var properties = type
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.CanWrite && x.PropertyType == typeof(string) && x.DeclaringType == type && x.PropertyType.GetCustomAttribute<NotMappedAttribute>() == null)
                    .ToList();

                foreach (var property in properties)
                {
                    var hasMaxLength = property.GetCustomAttribute<MaxLengthAttribute>(true) != null;
                    var notMapped = property.GetCustomAttribute<NotMappedAttribute>() != null;

                    var propertyFullName = $"{property.DeclaringType!.FullName}.{property.Name}";

                    Assert.True(notMapped || hasMaxLength || allowedPropertiesWithoutMaxLength.Any(x => propertyFullName.EndsWith(x)),
                        $"A string property {propertyFullName} has no MaxLengthAttribute.");
                }
            }
        }
    }
}