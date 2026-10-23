using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Pharos.Server.Application.Settings;
using System.ComponentModel.DataAnnotations;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

internal abstract class SettingsVisitor
{
    private const BindingFlags _declaredOnlyLookup = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const int _maxConfigSectionHierarchyDepth = 20;

    public bool? IsDevelopment { get; set; }

    protected void VisitAssemblies(IEnumerable<Assembly> assemblies, Func<Assembly, IEnumerable<Type>>? getConfigSections = null)
    {
        var configRoots = new List<Type>();

        foreach (var assembly in assemblies)
        {
            var types = getConfigSections != null ? getConfigSections(assembly)
                : assembly.GetTypes().Where(t => t.CustomAttributes.Any(x => x.AttributeType == typeof(RootConfigSectionAttribute)));

            foreach (var type in types)
            {
                CheckConfigSectionTypeIsValid(type);
                configRoots.Add(type);
            }
        }

        foreach (var section in configRoots)
        {
            var instance = Activator.CreateInstance(section)!;
            var initializedSection = false;

            if (IsDevelopment != null && instance is IConfigSectionWithInitialValues initialConfig)
            {
                initialConfig.InitializeConfigSection((bool)IsDevelopment);
                initializedSection = true;
            }

            VisitObject(null, section, instance, initializedSection);
        }
    }

    protected List<Type> GetRootConfigs(IEnumerable<Assembly> assemblies)
    {
        var configRoots = new List<Type>();
        foreach (var assembly in assemblies)
        {
            var types = assembly.GetTypes().Where(t => t.CustomAttributes.Any(x => x.AttributeType == typeof(RootConfigSectionAttribute)));

            foreach (var type in types)
            {
                CheckConfigSectionTypeIsValid(type);
                configRoots.Add(type);
            }
        }
        return configRoots;
    }

    protected virtual void VisitArray(PropertyInfo property, Type elementType, string keyPrefix, object? array, bool initializedSection = false)
    {
        if (array == null)
        {
            return;
        }

        // Validate unique constraints within the array for seeded sections
        if (initializedSection)
        {
            ValidateUniqueConstraints(property, elementType, array);
        }

        var index = 0;
        foreach (var item in (IEnumerable)array)
        {
            VisitArrayElement(elementType, keyPrefix + ":" + index, item, initializedSection);
            index++;
        }
    }

    private static void ValidateUniqueConstraints(PropertyInfo arrayProperty, Type elementType, object array)
    {
        var uniqueProperties = elementType.GetProperties()
            .Where(p => p.GetCustomAttribute<UniqueAttribute>() != null)
            .ToList();

        if (uniqueProperties.Count == 0)
        {
            return;
        }

        var items = ((IEnumerable)array).Cast<object>().ToList();

        foreach (var uniqueProperty in uniqueProperties)
        {
            var attr = uniqueProperty.GetCustomAttribute<UniqueAttribute>()!;
            var comparer = attr.CaseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

            var values = items.Select(item => uniqueProperty.GetValue(item)?.ToString()).ToList();

            var duplicates = values
                .GroupBy(v => v, comparer)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicates.Count > 0)
            {
                var duplicateValues = string.Join(", ", duplicates.Select(d => d ?? "null"));
                throw new ValidationException(
                    $"The initial list '{arrayProperty.Name}' contains duplicate values for property '{uniqueProperty.Name}' which is marked as [Unique]. Duplicate values: {duplicateValues}");
            }
        }
    }

    protected virtual void VisitObject(string? parentPath, Type objectType, object instance, bool initializedSection = false)
    {
        Debug.Assert(ConfigurationPath.KeyDelimiter == ":");
        if ((parentPath?.Count(c => c == ':') ?? 0) >= _maxConfigSectionHierarchyDepth)
        {
            throw new InvalidOperationException($"The maximum depth of {_maxConfigSectionHierarchyDepth} object nesting has been reached. Check for circular references between types.");
        }

        var currentPath = parentPath;
        if (string.IsNullOrEmpty(currentPath))
        {
            currentPath = SettingsServiceCollectionExtensions.GetConfigSectionName(objectType);
        }

        var properties = GetAllReadWriteProperties(objectType);

        foreach (var property in properties)
        {
            var type = property.PropertyType;
            var propertyPath = currentPath + ":" + property.Name;
            if (type == typeof(SettingsAction))
            {
                VisitAction(property, propertyPath, property.GetValue(instance, null) as SettingsAction);
            }
            else if (TypeIsValue(type))
            {
                var value = property.GetValue(instance, null);

                // validate seeded values so no invalid values go to DB during seeding.
                if (!ValueValidator.IsValidValue(property, value))
                {
                    var msg = initializedSection
                        ? $"The seeded value of property {property.Name} in {objectType.FullName} has an invalid value."
                        : $"Property {property.Name} in {objectType.FullName} has an invalid value.";

                    throw new ValidationException(msg);
                }

                VisitValue(property, propertyPath, value, null);
            }
            else if (TypeIsDictionaryInterface(type) || type.IsPrimitive || type == typeof(DateTime) || type == typeof(DateTime?))
            {
                throw new NotSupportedException($"Property {property.Name} in {objectType.FullName} has an unsupported type {type.Name}.");
            }
            else if (TypeIsArrayOrCompatible(type, out var elementType))
            {
                var array = property.GetValue(instance, null);
                VisitArray(property, elementType!, propertyPath, array, initializedSection);
            }
            else if (!type.IsAbstract)
            {
                var propInstance = property.GetValue(instance, null);

                // When we are initializing setting we only want to visit object properties
                // that are initialized after IConfigSectionWithInitialValues.InitializeConfigSection() call.
                if (propInstance == null && !initializedSection)
                {
                    propInstance = Activator.CreateInstance(type)!;
                }

                if (propInstance != null)
                {
                    VisitObject(propertyPath, type, propInstance, initializedSection);
                }
            }
        }
    }


    protected virtual void VisitValue(PropertyInfo? property, string key, object? value, object? defaultValue) { }

    protected virtual void VisitAction(PropertyInfo property, string key, SettingsAction? action) { }

    protected virtual void CheckConfigSectionTypeIsValid(Type type)
    {
        if (type.IsAbstract)
        {
            throw new InvalidOperationException($"{type.FullName} is an abstract class. Abstract classes cannot be used as as configuration sections.");
        }
        if (type.GetConstructor([]) == null)
        {
            throw new InvalidOperationException($"{type.FullName} has no default constructor. A configuration section must have a default constructor.");
        }
    }

    private void VisitArrayElement(Type elementType, string key, object? value, bool initializedSection = false)
    {
        if (value == null || TypeIsValue(elementType))
        {
            VisitValue(null, key, value, null);
            return;
        }
        VisitObject(key, elementType, value, initializedSection);
    }

    internal static List<PropertyInfo> GetAllReadWriteProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
    {
        var allProperties = new List<PropertyInfo>();

        var baseType = type;
        while (baseType != typeof(object))
        {
            var properties = baseType.GetProperties(_declaredOnlyLookup);

            foreach (var property in properties)
            {
                // if the property is virtual, only add the base-most definition so
                // overridden properties aren't duplicated in the list.
                var setMethod = property.GetSetMethod(true);

                if (setMethod is not null && !setMethod.IsPrivate && (!setMethod.IsVirtual || setMethod == setMethod.GetBaseDefinition()))
                {
                    allProperties.Add(property);
                }
            }

            baseType = baseType.BaseType!;
        }

        return allProperties;
    }

    protected static bool TypeIsArrayOrCompatible(Type type, out Type? elementType)
    {
        elementType = null;
        if (type.IsArray)
        {
            elementType = type.GetElementType();
            return true;
        }

        if (IsImmutableArrayCompatibleInterface(type))
        {
            elementType = type.GenericTypeArguments[0];
            return true;
        }

        var collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);
        if (collectionInterface != null)
        {
            elementType = type.GenericTypeArguments[0];
            return true;
        }

        return false;
    }

    private static bool IsImmutableArrayCompatibleInterface(Type type)
    {
        if (!type.IsInterface || !type.IsConstructedGenericType) { return false; }

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(IEnumerable<>)
               || genericTypeDefinition == typeof(IReadOnlyCollection<>)
               || genericTypeDefinition == typeof(IReadOnlyList<>);
    }


    private static Type? FindOpenGenericInterface(
        Type expected,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
        Type actual)
    {
        if (actual.IsGenericType &&
            actual.GetGenericTypeDefinition() == expected)
        {
            return actual;
        }

        var interfaces = actual.GetInterfaces();
        foreach (var interfaceType in interfaces)
        {
            if (interfaceType.IsGenericType &&
                interfaceType.GetGenericTypeDefinition() == expected)
            {
                return interfaceType;
            }
        }
        return null;
    }

    protected static bool TypeIsDictionaryInterface(Type type)
    {
        if (!type.IsInterface || !type.IsConstructedGenericType) { return false; }

        var genericTypeDefinition = type.GetGenericTypeDefinition();
        return genericTypeDefinition == typeof(IDictionary<,>)
               || genericTypeDefinition == typeof(IReadOnlyDictionary<,>);
    }

    internal static bool TypeIsValue(Type type)
    {
        return type == typeof(string) || type == typeof(bool) || type == typeof(int) || type == typeof(double)
               || type == typeof(bool?) || type == typeof(int?) || type == typeof(double?) || type == typeof(DateTimeOffset)
               || type == typeof(DateTimeOffset?) || type.IsEnum || type == typeof(TimeOnly);
    }
}
