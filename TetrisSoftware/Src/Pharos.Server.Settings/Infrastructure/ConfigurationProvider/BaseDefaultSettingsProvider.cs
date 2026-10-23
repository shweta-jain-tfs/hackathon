using System.Reflection;
using Pharos.Server.Settings.Application.Metadata;
using Pharos.Server.Settings.Application.Services;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider
{
    internal sealed class BaseDefaultSettingsProvider : BaseSettingsPagesProvider
    {
        private readonly Dictionary<string, (string? Value, bool isPageStructure)> _defaultSettings = new();
        private readonly Dictionary<string, Option> _defaultOptions = new();

        private BaseDefaultSettingsProvider(IValueConverter valueConverter)
            : base(valueConverter)
        {
        }

        private static BaseDefaultSettingsProvider CreateProvider()
        {
            return new(new ValueConverter());
        }

        public static Dictionary<string, Page> GetTopLevelPages()
        {
            if (!_assemblies.Any())
            {
                return [];
            }

            return CreateProvider().VisitAssembliesAndGetPages();
        }
        public static IDictionary<string, (string?, bool)> GetDefaultSettingsAndPageStructure()
        {
            var provider = CreateProvider();
            provider.VisitAssembliesAndGetPages();

            return provider._defaultSettings;
        }

        public static IDictionary<string, Option> GetDefaultOptions()
        {
            var provider = CreateProvider();
            provider.VisitAssembliesAndGetPages();

            return provider._defaultOptions;
        }

        public static ImportExportExclusions GetImportExportExclusions()
        {
            var provider = CreateProvider();
            var pages = provider.VisitAssembliesAndGetPages();

            var prefixes = new HashSet<string>(StringComparer.Ordinal);
            var exactKeys = new HashSet<string>(StringComparer.Ordinal);
            CollectImportExportExclusions(pages.Values, prefixes, exactKeys);
            return new ImportExportExclusions(prefixes, exactKeys);
        }

        private static void CollectImportExportExclusions(IEnumerable<Page> pages, HashSet<string> prefixes, HashSet<string> exactKeys)
        {
            foreach (var page in pages)
            {
                CollectImportExportExclusionsFromItem(page, prefixes, exactKeys);
            }
        }

        private static void CollectImportExportExclusionsFromChildren(IEnumerable<SettingItemBase> children, HashSet<string> prefixes, HashSet<string> exactKeys)
        {
            foreach (var child in children)
            {
                CollectImportExportExclusionsFromItem(child, prefixes, exactKeys);
            }
        }

        private static void CollectImportExportExclusionsFromItem(SettingItemBase item, HashSet<string> prefixes, HashSet<string> exactKeys)
        {
            if (item is ActionButton)
            {
                exactKeys.Add(item.Key);
                return;
            }

            if (item.ExcludeFromImportExport)
            {
                if (item is Option)
                {
                    exactKeys.Add(item.Key);
                }
                else
                {
                    prefixes.Add(item.Key + PageHelper.SettingsKeyDeLimiter);
                }

                return;
            }

            switch (item)
            {
                case PageList pageList:
                    if (pageList.PageStructure is Page structurePage)
                    {
                        CollectImportExportExclusionsFromItem(structurePage, prefixes, exactKeys);
                    }

                    CollectImportExportExclusionsFromChildren(pageList.PageEntries, prefixes, exactKeys);
                    break;
                case Page page:
                    CollectImportExportExclusionsFromChildren(page.Children, prefixes, exactKeys);
                    break;
                case Group group:
                    CollectImportExportExclusionsFromChildren(group.Children, prefixes, exactKeys);
                    break;
                case PagePlaceholder pagePlaceholder:
                    CollectImportExportExclusionsFromChildren(pagePlaceholder.Options, prefixes, exactKeys);
                    break;
            }
        }

        protected override void VisitValue(PropertyInfo? property, string key, object? value, object? defaultValue)
        {
            if (property == null)
            {
                return;
            }

            var option = CreateOptionFromProperty(property, key, value, defaultValue);
            PageHelper.SetDependency(property, key, option, _currentKeys);

            // Add for dependencies
            _currentKeys.Add(key);
            _currentParent?.Children.Add(option);

            _defaultSettings.Add(key, (ValueFormattingHelper.ValueToString(defaultValue), IsPageStructure));
            _defaultOptions.Add(key, option);
        }
    }
}
