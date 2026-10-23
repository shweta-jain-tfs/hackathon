using System.Reflection;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider;

internal class DefaultSettingsProvider : SettingsVisitor
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string TimeFormat = "HH:mm";
    private readonly Dictionary<string, string?> _settings = new();

    private DefaultSettingsProvider() { }
    public static IDictionary<string, string?> GetDefaultSettings(IEnumerable<Assembly> assemblies, Func<Assembly, IEnumerable<Type>>? getConfigSections = null, bool? isDevelopment = null)
    {
        var provider = new DefaultSettingsProvider
        {
            IsDevelopment = isDevelopment
        };
        provider.VisitAssemblies(assemblies, getConfigSections);

        return provider._settings;
    }

    protected override void VisitValue(PropertyInfo? property, string key, object? value, object? defaultValue)
    {
        _settings.Add(key, ValueToString(value));
    }

    internal static string? ValueToString(object? value)
    {
        return ValueFormattingHelper.ValueToString(value);
    }
}
