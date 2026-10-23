using Microsoft.Extensions.Localization;
using Pharos.Server.Settings.Application.Metadata;

namespace Pharos.Server.Settings.Infrastructure.ConfigurationProvider
{
    internal sealed class SettingsSearchPagesProvider : BaseSettingsPagesProvider
    {
        private SettingsSearchPagesProvider(IValueConverter valueConverter, IStringLocalizer localizer)
            : base(valueConverter, localizer)
        {
        }

        private static SettingsSearchPagesProvider CreateProvider(IStringLocalizer localizer)
        {
            return new(new ValueConverter(), localizer);
        }

        public static Dictionary<string, Page> GetTopLevelPages(IStringLocalizer localizer)
        {
            if (!_assemblies.Any())
            {
                return [];
            }

            return CreateProvider(localizer).VisitAssembliesAndGetPages();
        }

        protected override void AddPageToCollection(Page page)
        {
            // Skip adding inner pages to _pages collection to speed up search
        }
    }
}
