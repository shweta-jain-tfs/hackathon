using System.Globalization;
using System.Reflection;
using Microsoft.IdentityModel.Tokens;
using Pharos.Server.Application.Settings.Attributes;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Application.Services;
internal static class PageHelper
{
    public const string SettingsKeyDeLimiter = ":";
    public const string SettingsLabelDeLimiter = "/";
    public const string SettingsAsteriskLineAfterHeader = "***********************************";

    internal static string GetSuffixKeyName(string key)
    {
        var lastIndexOfDelimiter = key.LastIndexOf(SettingsKeyDeLimiter, StringComparison.InvariantCulture);
        return key[(lastIndexOfDelimiter + 1)..];
    }

    internal static string GetSuffixKeyNameTableItem(string key)
    {
        var lastIndexOfDelimiter = key.LastIndexOf(SettingsKeyDeLimiter, StringComparison.InvariantCulture);
        var secondLastIndexOfDelimiter = key.LastIndexOf(SettingsKeyDeLimiter, lastIndexOfDelimiter - 1, StringComparison.InvariantCulture);
        return key[(secondLastIndexOfDelimiter + 1)..];
    }

    internal static int GetListEntryIndex(string key, string parentKey)
    {
        var afterParent = key.Substring(parentKey.Length + 1); // including ":"
        var index = afterParent[..afterParent.IndexOf(PageHelper.SettingsKeyDeLimiter, StringComparison.InvariantCulture)];
        return int.Parse(index, CultureInfo.InvariantCulture);
    }

    internal static int GetCommonPrefixLength(string str1, string str2)
    {
        var minLength = Math.Min(str1.Length, str2.Length);
        var commonLength = 0;

        for (var i = 0; i < minLength; i++)
        {
            if (str1[i] != str2[i])
            {
                break;
            }
            commonLength++;
        }

        return commonLength;
    }

    internal static int CountDistinctPageListItemIndices(IEnumerable<string> keys, string pageListKey)
    {
        var prefix = pageListKey + SettingsKeyDeLimiter;
        var indices = new HashSet<int>();

        foreach (var key in keys)
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal))
            {
                continue;
            }

            var remainder = key[prefix.Length..];
            var firstDelimiterIndex = remainder.IndexOf(SettingsKeyDeLimiter, StringComparison.Ordinal);
            if (firstDelimiterIndex < 0)
            {
                continue;
            }

            var indexSegment = remainder[..firstDelimiterIndex];
            if (int.TryParse(indexSegment, NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                indices.Add(index);
            }
        }

        return indices.Count;
    }

    internal static List<Dependency> CopyDependency(List<Dependency>? dependencies, string newKeyPrefix)
    {
        List<Dependency> newDependencies = [];
        if (dependencies != null)
        {
            foreach (var dependency in dependencies)
            {
                if (dependency is { MainOptionKey: not null })
                {
                    var commonLength = GetCommonPrefixLength(newKeyPrefix, dependency.MainOptionKey);
                    var prefix = newKeyPrefix.Substring(0, commonLength);
                    var idx = newKeyPrefix.Substring(commonLength).Split(SettingsKeyDeLimiter).First();
                    var suffix = dependency.MainOptionKey.Substring(commonLength);
                    var dependencyKey = prefix + idx + SettingsKeyDeLimiter + suffix;

                    var newDependency = new Dependency
                    {
                        Type = dependency.Type,
                        MainOptionKey = dependencyKey,
                        MainOptionValue = dependency.MainOptionValue,
                    };
                    newDependencies.Add(newDependency);
                }
            }
        }

        return newDependencies;
    }

    internal static string CopySettingKey(string key, string newKeyPrefix)
    {
        if (string.IsNullOrEmpty(newKeyPrefix))
        {
            return key;
        }

        var commonLength = GetCommonPrefixLength(newKeyPrefix, key);
        var prefix = newKeyPrefix.Substring(0, commonLength);
        var idx = newKeyPrefix.Substring(commonLength).Split(SettingsKeyDeLimiter).First();
        var suffix = key.Substring(commonLength);
        return prefix + idx + SettingsKeyDeLimiter + suffix;
    }

    /// <summary>
    /// Sets the dependencies for a given setting item based on the DependsFromAttribute.
    /// </summary>
    /// <param name="property">The property info of the setting item.</param>
    /// <param name="propertyPath">The path of the property in the settings hierarchy.</param>
    /// <param name="item">The setting item to which dependencies will be added.</param>
    /// <param name="currentKeys">The list of current keys in the settings hierarchy.</param>
    internal static void SetDependency(PropertyInfo? property, string propertyPath, SettingItemBase item, List<string> currentKeys)
    {
        var dependsFromAttribute = property?.GetCustomAttribute<DependsFromAttribute>();

        if (dependsFromAttribute == null)
        {
            return;
        }

        var dependsFrom = property?.GetCustomAttribute<DependsFromAttribute>()?.Property.ToList();
        var mainOptionValue = property?.GetCustomAttribute<DependsFromAttribute>()?.Value.ToList();
        var dependencyType = property?.GetCustomAttribute<DependsFromAttribute>()?.DependencyType;

        if (dependsFrom.IsNullOrEmpty() || mainOptionValue.IsNullOrEmpty())
        {
            throw new ArgumentException($"Property or Value cannot be empty list in '{property?.Name}' DependsFromAttribute");
        }

        if (dependsFrom != null && mainOptionValue != null && dependencyType != null)
        {
            if (dependsFrom.Count != mainOptionValue.Count)
            {
                throw new ArgumentException($"The number of Properties in '{property?.Name}' must match the number of Values defined in the DependsFromAttribute.");
            }

            var mainOptionKeys = new List<string>();

            foreach (var depends in dependsFrom)
            {
                var mainOptionKey = string.Concat(propertyPath.AsSpan(0, propertyPath.Length - (property?.Name.Length ?? 0)), depends);
                mainOptionKeys.Add(mainOptionKey);
            }

            if (mainOptionKeys.Any(key => !currentKeys.Contains(key)))
            {
                throw new ArgumentException("Depending from: " + dependsFrom + ", is not supported");
            }

            List<Dependency> dependencies = [];
            for (var i = 0; i < mainOptionKeys.Count; i++)
            {
                var dependency = new Dependency
                {
                    MainOptionKey = mainOptionKeys[i],
                    MainOptionValue = mainOptionValue[i],
                    Type = dependencyType,
                };

                dependencies.Add(dependency);
            }

            item.Dependency = dependencies;
        }
    }

    /// <summary>
    /// Checks if the given setting is currently hidden in the page structure.
    /// </summary>
    /// <param name="page"></param>
    /// <param name="setting"></param>
    /// <param name="checkDependencyType">
    /// If <c>true</c>, only evaluates dependencies of type <see cref="DependencyType.Hide"/> 
    /// If <c>false</c>, ignores the dependency type and checks if any dependencies exist.
    /// </param>
    /// <returns></returns>
    internal static bool IsHidden(Page page, SettingItemBase setting, bool checkDependencyType = true)
    {
        if (setting.Dependency.IsNullOrEmpty())
        {
            return false;
        }
        if (checkDependencyType && setting.Dependency!.First().Type != DependencyType.Hide)
        {
            return false;
        }

        var options = FindOptions(page);
        return setting.Dependency!
            .GroupBy(dep => dep.MainOptionKey)
            .Any(group => !group.Any(dep => options[dep.MainOptionKey].Value == dep.MainOptionValue));
    }

    /// <summary>
    /// Finds all the options relative to a page by traversing its structure.
    /// </summary>
    /// <param name="page"></param>
    /// <returns></returns>
    internal static Dictionary<string, Option> FindOptions(Page page)
    {
        return FindOptions((ISettingsParentItem)page);
    }

    internal static Dictionary<string, Option> FindOptions(ISettingsParentItem parent)
    {
        var options = new Dictionary<string, Option>();
        VisitParent(parent, options);
        return options;
    }

    private static void VisitParent(ISettingsParentItem parent, Dictionary<string, Option> options)
    {
        foreach (var child in parent.Children)
        {
            VisitChild(child, options);
        }
    }

    private static void VisitChild(SettingItemBase child, Dictionary<string, Option> options)
    {
        switch (child)
        {
            case var _ when child is Group group:
                VisitGroup(group, options);
                break;

            case var _ when child is Page page:
                VisitPage(page, options);
                break;

            case var _ when child is PageList pageList:
                foreach (var pageEntry in pageList.PageEntries)
                {
                    if (pageEntry is Page entryPage)
                    {
                        VisitPage(entryPage, options);
                    }
                    else if (pageEntry is PagePlaceholder placeholder)
                    {
                        foreach (var placeholderOption in placeholder.Options)
                        {
                            VisitOption(placeholderOption, options);
                        }
                    }
                }
                break;

            case var _ when child is Option option:
                VisitOption(option, options);
                break;
        }
    }

    private static void VisitPage(Page page, Dictionary<string, Option> options)
    {
        VisitParent(page, options);
    }

    private static void VisitOption(Option option, Dictionary<string, Option> options)
    {
        options.TryAdd(option.Key, option);
    }

    private static void VisitGroup(Group group, Dictionary<string, Option> options)
    {
        VisitParent(group, options);
    }
}
