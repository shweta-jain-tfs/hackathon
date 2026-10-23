using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings;

public static class SettingsServiceCollectionExtensions
{
    public const string OptionsSuffix = "Options";
    public const string SettingsSuffix = "Settings";

    public static IServiceCollection ConfigureSettings<TSettings>(this IServiceCollection services, IConfiguration configuration) where TSettings : class
    {
        var path = GetSectionPath<TSettings>(configuration);

        services.Configure<TSettings>(configuration.GetSection(path));

        return services;
    }

    public static string GetSectionPath<TSettings>(IConfiguration configuration) where TSettings : class
    {
        var type = typeof(TSettings);
        var sectionName = GetConfigSectionName(type);
        string? path = null;

        if (type.CustomAttributes.Any(x => x.AttributeType == typeof(RootConfigSectionAttribute)))
        {
            path = sectionName;
        }

        if (path == null)
        {
            GetSectionPath<TSettings>(configuration, sectionName, ref path);
            if (path == null)
            {
                throw new InvalidOperationException($"Cannot bind a configuration section {typeof(TSettings).FullName}: no settings found in configuration. Make sure that sections are properly decorates with {nameof(RootConfigSectionAttribute)}.");
            }
        }

        return path;
    }

    public static string GetConfigSectionName(Type sectionType)
    {
        var name = sectionType.Name;

        if (name.Length > SettingsSuffix.Length && name.EndsWith(SettingsSuffix))
        {
            name = name[..^SettingsSuffix.Length];
        }
        else if (name.Length > OptionsSuffix.Length && name.EndsWith(OptionsSuffix))
        {
            name = name[..^OptionsSuffix.Length];
        }

        return name;
    }

    private static void GetSectionPath<TSettings>(IConfiguration configuration, string sectionName, ref string? path)
    {
        foreach (var section in configuration.GetChildren())
        {
            if (section.Key == sectionName)
            {
                if (path != null)
                {
                    throw new InvalidOperationException($"Cannot bind a configuration section {typeof(TSettings).FullName}: section name is not unique. Ambiguous paths: {path} and {section.Path}.");
                }

                if (section.Value != null || !section.GetChildren().Any())
                {
                    throw new InvalidOperationException($"Configuration section {typeof(TSettings).FullName} cannot be bound to a single value {section.Path}");
                }

                path = section.Path;
            }
            GetSectionPath<TSettings>(section, sectionName, ref path);
        }
    }
}
