using System.Globalization;
using Pharos.Contracts.Security;
using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.General;

[RootConfigSection(sortOrder: 10)]
[DisplayNameLocal("GeneralSettings")]
public class GeneralSettings
{
    [SettingsGroup]
    [DisplayNameLocal("GeneralSettings_RegionalSettings")]
    public RegionalSettings RegionalSettings { get; set; } = new();
    [SettingsGroup]
    [Permission(Permissions.EditSystemSettings)]
    [DisplayNameLocal("GeneralSettings_ProductsSettings")]
    public ProductsSettings ProductsSettings { get; set; } = new();

    public CultureInfo GetCultureInfoFromSetting()
    {
        var cultureName = RegionalSettings.Culture.ToCultureName();
        return CultureInfo.GetCultureInfo(cultureName);
    }
}