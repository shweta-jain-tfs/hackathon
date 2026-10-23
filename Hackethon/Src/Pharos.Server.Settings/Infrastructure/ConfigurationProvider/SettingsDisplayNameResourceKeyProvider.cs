using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

internal sealed class SettingsDisplayNameResourceKeyProvider : BaseSettingsPagesProvider
{
    private static readonly Lock _cacheLock = new();
    private static HashSet<string>? _cachedDisplayNameResourceKeys;
    private static string _cachedContextKey = string.Empty;

    private readonly HashSet<string> _displayNameResourceKeys = new(StringComparer.Ordinal);

    private SettingsDisplayNameResourceKeyProvider(IValueConverter valueConverter)
        : base(valueConverter)
    {
    }

    public static IReadOnlySet<string> GetDisplayNameResourceKeys(Func<Assembly, IEnumerable<Type>>? getConfigSections = null)
    {
        var currentContextKey = CreateContextKey(getConfigSections);
        if (_cachedDisplayNameResourceKeys is not null && string.Equals(_cachedContextKey, currentContextKey, StringComparison.Ordinal))
        {
            return _cachedDisplayNameResourceKeys;
        }

        lock (_cacheLock)
        {
            currentContextKey = CreateContextKey(getConfigSections);
            if (_cachedDisplayNameResourceKeys is not null && string.Equals(_cachedContextKey, currentContextKey, StringComparison.Ordinal))
            {
                return _cachedDisplayNameResourceKeys;
            }

            if (!_assemblies.Any())
            {
                _cachedDisplayNameResourceKeys = [];
                _cachedContextKey = currentContextKey;
                return _cachedDisplayNameResourceKeys;
            }

            var provider = new SettingsDisplayNameResourceKeyProvider(new ValueConverter());
            provider.VisitAssembliesAndGetPages(getConfigSections);

            _cachedDisplayNameResourceKeys = provider._displayNameResourceKeys;
            _cachedContextKey = currentContextKey;
            return _cachedDisplayNameResourceKeys;
        }
    }

    public static void PreloadDisplayNameResourceKeys()
    {
        _ = GetDisplayNameResourceKeys();
    }

    internal static void ResetCache()
    {
        lock (_cacheLock)
        {
            _cachedDisplayNameResourceKeys = null;
            _cachedContextKey = string.Empty;
        }
    }

    protected override string GetTopLevelPageLabel(Type objectType)
    {
        var resourceKey = base.GetTopLevelPageLabel(objectType);
        AddDisplayNameResourceKey(resourceKey);
        return resourceKey;
    }

    protected override string GetPropertyLabel(PropertyInfo property)
    {
        var resourceKey = base.GetPropertyLabel(property);
        AddDisplayNameResourceKey(resourceKey);
        return resourceKey;
    }

    protected override string GetLocalizedText(string resourceKey)
    {
        return resourceKey;
    }

    private static string CreateContextKey(Func<Assembly, IEnumerable<Type>>? getConfigSections)
    {
        var assemblyPart = string.Join('|', _assemblies.Select(static assembly => assembly.FullName));
        var filter = getConfigSections ?? _getConfigSections;
        var filterPart = filter is null
            ? "none"
            : RuntimeHelpers.GetHashCode(filter).ToString(CultureInfo.InvariantCulture);

        return $"{assemblyPart}::{filterPart}";
    }

    private void AddDisplayNameResourceKey(string resourceKey)
    {
        if (!string.IsNullOrWhiteSpace(resourceKey))
        {
            _displayNameResourceKeys.Add(resourceKey);
        }
    }
}
